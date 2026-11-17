# BuildTimestamp: Consistency Across Multi-Project Builds

In a large solution, compilation of later projects can happen minutes after earlier ones. Left unchecked, each project would stamp its own `HHmm` revision at the moment *it* compiles, producing different versions within a single build.

## How it's solved

`BuildTimestamp` is captured during MSBuild **property evaluation**, which happens before any project compiles, so the value does not depend on how long compilation itself takes. In a **Debug** build it is held to the day — midnight, in the stamp's own format — so every project in the build takes one value exactly. Every other configuration, Release among them, captures the current time instead.

> **The stamp is captured once per project *evaluation*. Whether that is once per build depends on how the build is run.** Measured on the four projects below, clock-stamped in Release, SDK 11.0.100:
>
> | How the build ran | Distinct stamps across the four projects |
> |---|---|
> | `dotnet build <solution>` — default parallelism | **One value, in 16 builds of 16**, including 6 with a 4-second delay inserted into `Core`, which the other three reference. MSBuild evaluates the solution's projects together, before any of them builds |
> | `dotnet publish <project>`, with or without `-m:1` | One value |
> | `dotnet build <solution> -m:1` — single node | **Two or three values, in 24 builds of 24**, about a second apart: `Leaf`, built last because it references the others, evaluates after `Core` has finished building. **One of those 24 straddled a `:00` boundary, so two projects took different `HHmm` minutes — two different versions inside one build** |
>
> `-m:1` is what this repository's own test fixture passes, for log attributability. A consumer does not pass it, and in the configuration a consumer does use, the projects agreed on every build measured.
>
> ⚠ **Earlier editions of this document drew general conclusions from measurements that never named their configuration** — first that all projects "normally land on the same value", then that most builds disagree. Neither survives the table above, and the table is four small projects rather than the large solution this document opens by describing: a parallel build of one may still reach projects minutes apart, which is not measured here.
>
> Two things hold regardless of build shape: the day-held Debug stamp is exact, and a `-p:BuildTimestamp` override is exact in every configuration. **If every project must carry one version whatever the build's shape, set the stamp yourself.**

If you override `BuildTimestamp` yourself (CI or `Build.ps1.template`), capture it as **local time on the same machine that runs the build** — the generator parses it with `DateTimeStyles.AssumeLocal`, so a UTC timestamp (e.g. `date -u`) would be misinterpreted unless that machine's local zone is already UTC. This is a non-issue for the shipped script and README examples, which are already local-time end to end; it only matters if you hand-roll a custom override.

## Why Debug holds its stamp to the day

A stamp is an input to compilation, so a new stamp is a reason to compile. The generator writes it into the assembly, which means MSBuild writes it into `obj/…/<project>.GeneratedMSBuildEditorConfig.editorconfig`, which `CoreCompile` counts among its inputs — and a recompiled project produces a new reference assembly, which is an input to every project referencing it. A fresh stamp on every build therefore rebuilds the whole graph every time, whether or not a line of code changed.

Measured on four projects, `Core` ← `Model` ← `App`, with `Leaf` referencing `Model` and `Core`:

| A Debug build after | Stamp per build | Stamp held to the day |
|---|---|---|
| nothing changed | 4 of 4 compiled | **0 of 4** |
| a method body edited in `Leaf`, which nothing references | 4 of 4 | **1 of 4** |
| a method body edited in `Core`, which the other three reference | 4 of 4 | **1 of 4** |

Debug is the inner loop — build, test, run, repeat over a tree that changed by a line or not at all — and the day is the coarsest hold that costs nothing: `AutoPackageVersion` is already only precise to the day, so holding to it leaves the package version exactly as it was. A finer hold recompiles everything whenever it ticks over, which is what the per-minute stamp was already doing.

Release keeps a stamp per build deliberately. It is the build that leaves the machine, usually from a clean checkout that compiles everything regardless, so holding it would cost the version its meaning and save that build nothing.

### What the hold costs

Two Debug builds made on one day carry the same version and the same `BuildRelease`, which reads `00:00:00` that day. That is the stamp reported faithfully — it is **not** the instant the build happened, and nothing recovers that instant from the assembly. A project that needs its own build time, such as a service logging its version at start-up, can set `BuildTimestamp` for itself; see the [Configuration Reference](../README.md#configuration-reference).

## Alternatives considered

| Approach | Guarantees | Limitations |
|---|---|---|
| **Day-held Debug stamp** (used here) | Exact across every project in a Debug build, and an unchanged build compiles nothing | Debug loses the time of day: `BuildRelease` reads `00:00:00` |
| **Property evaluation** (used here for every other configuration) | Evaluated well before compilation starts, so it does not drift with compile time | Not one value per build: a project takes its stamp when the build first asks for it, so a build can carry two or three. Can also cross a minute boundary on deep dependency chains, which changes the version itself |
| **`-p:BuildTimestamp=...` override** | Exact — one value, computed once, passed to every project before evaluation starts | Requires a wrapper script or CI step; not available for IDE "Build Solution" clicks |
| **A stamp that stays still unless something changed** | Would hold across days, not only within one | An edit low in the graph still recompiles everything above it, because a project's new stamp is a new reference assembly; and MSBuild reads `AutoVersion` as empty, so a package could no longer take its version from the stamp inside it |
| **Static field in the generator** | No IPC needed | Relies on undocumented Roslyn compiler-server process reuse; goes stale across unrelated builds in the IDE (long-lived process); races under parallel builds |
| **Named mutex / memory-mapped file** | True cross-process shared memory | Not reliably cross-platform; still needs a "new build session" signal that doesn't exist |
| **Auto-triggered file lock (custom MSBuild Task)** | Automates coordination — no manual script needed | Needs an arbitrary expiry window: too short re-splits slow builds, too long lets a crashed build's leftover file poison the next one |
| **`before/after.<sln>.targets` hook** | Genuine one-time hook for CLI `.sln` builds | Can't be auto-injected by a NuGet package; not honored by Rider/Visual Studio's own IDE build orchestrators |

For CI, or any build where an exact guarantee matters in a configuration other than Debug, use the `-p:BuildTimestamp=...` override — see the [Configuration Reference](../README.md#configuration-reference) in the main README. Ready-to-use implementations of this override — including `CommitSha` and `PublicVersion` for local builds — are available at [`Build.ps1.template`](../Build.ps1.template) (Windows/PowerShell 7+) and [`Build.sh.template`](../Build.sh.template) (Linux/macOS/bash). An override wins over the hold, so a Debug build driven by one of those scripts takes a stamp per build and recompiles accordingly.
