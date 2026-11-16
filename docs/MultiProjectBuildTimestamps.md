# BuildTimestamp: Consistency Across Multi-Project Builds

In a large solution, compilation of later projects can happen minutes after earlier ones. Left unchecked, each project would stamp its own `HHmm` revision at the moment *it* compiles, producing different versions within a single build.

## How it's solved

`BuildTimestamp` is captured during MSBuild **property evaluation**, which happens before any project compiles, so the value does not depend on how long compilation itself takes. In a **Debug** build it is held to the day — midnight, in the stamp's own format — so every project in the build takes one value exactly. Every other configuration, Release among them, captures the current time instead.

> **A capture per evaluation is not one capture per build.** Measured over 24 clock-stamped builds, 19 stamped their projects with more than one value, up to three: a project takes its stamp when the build first asks for it, and a `dotnet publish` stamped the root library a second after the projects referencing it. None of those 24 crossed a minute, so the *version* — which is only precise to `HHmm` — agreed throughout. `DirectoryBuildInfo.BuildRelease`, which carries the seconds, mostly did not. The day-held Debug stamp is exact rather than merely close, and a `-p:BuildTimestamp` override is exact in any configuration.

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
