# BuildTimestamp: Consistency Across Multi-Project Builds

In a large solution, the build reaches its later projects long after its earlier ones. Left unchecked, each project stamps its own `HHmm` revision from the clock as the build arrives at it — and in a 150-project solution measured below, that is two assemblies whose versions are a minute apart inside one build, in 2 builds of 25.

## How it's solved

`BuildTimestamp` is captured during MSBuild **property evaluation**, not when a project compiles, so it does not depend on how long compilation takes. That is a *narrower* guarantee than it sounds, and the measurements below are what it is worth: evaluation is per project, and in a large solution it is interleaved with building rather than finished before it. In a **Debug** build the stamp is held to the day — midnight, in the stamp's own format — so every project in the build takes one value exactly, whatever the solution's size. Every other configuration, Release among them, captures the current time instead.

> **The stamp is captured once per project *evaluation*, and in a solution of any size that is not once per build.** A project's stamp is taken when the build first evaluates it, so the spread across a build is how long evaluation takes to reach the last project — **which grows with the number of projects.** Measured clock-stamped in Release on SDK 11.0.100, every project's report confirmed present in every run:
>
> | How the build ran | Projects | Distinct stamps in one build | Spread | Crossed a minute |
> |---|---|---|---|---|
> | `dotnet build <solution>`, default parallelism | **150** | **2 to 12; every one of 25 builds used more than one** | 1–11 s, mean 8 | **2 of 25 builds** |
> | `dotnet build <solution> -m:1`, single node | 4 | 2 or 3; every one of 24 builds | 1–2 s | 1 of 24 builds |
> | `dotnet build <solution>`, default parallelism | 4 | one value, all 16 builds | 0 | 0 of 16 |
> | `dotnet publish <project>`, with or without `-m:1` | 4 | one value, all 6 builds | 0 | 0 of 6 |
>
> **Four projects are too few to show it.** At that size a parallel build evaluates them together before any compiles — a 4-second delay forced into the project the other three reference could not separate them, across 6 builds. `-m:1` separates them only because it serialises evaluation behind each project's build, which is why this repository's test fixture passes it for log attributability. At 150 projects the spread appears without any flag.
>
> **What the spread costs, precisely:**
>
> - `DirectoryBuildInfo.BuildRelease` carries the seconds, so it **differs between projects on essentially every clock-stamped build of a large solution** — up to 12 values in one build here.
> - `AutoVersion` is precise to `HHmm` and `AutoPackageVersion` to the day, so those differ **only when the spread straddles a boundary** — observed in **2 of 25** builds, each producing assemblies whose versions are a minute apart inside a single build.
>
> ⚠ **Three earlier editions of this passage each generalised from a measurement whose configuration they never named** — that all projects "normally land on the same value", then that most builds disagree, then that the disagreement needs `-m:1`. The table above is the first to state project count and build mode for every number in it. What it still does not cover: projects that are slow to *compile*. These are one-type projects, so what was measured is the duration of evaluation.
>
> Two things hold regardless of solution size or build mode: the day-held Debug stamp is exact, and a `-p:BuildTimestamp` override is exact in every configuration. **So in a solution of any real size, pass `-p:BuildTimestamp` on the builds whose version must be uniform — Release and CI above all, which is where the clock is still taken per project and where the artifact leaves the machine.** `Build.ps1.template` and `Build.sh.template` already do; Debug needs nothing, being held to its day.

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
