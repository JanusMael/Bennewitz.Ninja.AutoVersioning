# BuildTimestamp: Consistency Across Multi-Project Builds

In a large solution, compilation of later projects can happen minutes after earlier ones. Left unchecked, each project would stamp its own `HHmm` revision at the moment *it* compiles, producing different versions within a single build.

## How it's solved

`BuildTimestamp` is captured once per project during MSBuild **property evaluation**, which happens before any project compiles, so all projects normally land on the same value regardless of how long compilation itself takes.

If you override `BuildTimestamp` yourself (CI or `Build.ps1.template`), capture it as **local time on the same machine that runs the build** — the generator parses it with `DateTimeStyles.AssumeLocal`, so a UTC timestamp (e.g. `date -u`) would be misinterpreted unless that machine's local zone is already UTC. This is a non-issue for the shipped script and README examples, which are already local-time end to end; it only matters if you hand-roll a custom override.

## Alternatives considered

| Approach | Guarantees | Limitations |
|---|---|---|
| **Property evaluation** (used here) | Matches across independent projects; evaluated well before compilation starts | Can still drift across a minute boundary on deep dependency chains, if upstream projects are slow to compile |
| **`-p:BuildTimestamp=...` override** | Exact — one value, computed once, passed to every project before evaluation starts | Requires a wrapper script or CI step; not available for IDE "Build Solution" clicks |
| **Static field in the generator** | No IPC needed | Relies on undocumented Roslyn compiler-server process reuse; goes stale across unrelated builds in the IDE (long-lived process); races under parallel builds |
| **Named mutex / memory-mapped file** | True cross-process shared memory | Not reliably cross-platform; still needs a "new build session" signal that doesn't exist |
| **Auto-triggered file lock (custom MSBuild Task)** | Automates coordination — no manual script needed | Needs an arbitrary expiry window: too short re-splits slow builds, too long lets a crashed build's leftover file poison the next one |
| **`before/after.<sln>.targets` hook** | Genuine one-time hook for CLI `.sln` builds | Can't be auto-injected by a NuGet package; not honored by Rider/Visual Studio's own IDE build orchestrators |

For CI, or any local build where an exact guarantee matters more than convenience, use the `-p:BuildTimestamp=...` override — see the [Configuration Reference](../README.md#configuration-reference) in the main README. Ready-to-use implementations of this override — including `CommitSha` and `PublicVersion` for local builds — are available at [`Build.ps1.template`](../Build.ps1.template) (Windows/PowerShell 7+) and [`Build.sh.template`](../Build.sh.template) (Linux/macOS/bash).
