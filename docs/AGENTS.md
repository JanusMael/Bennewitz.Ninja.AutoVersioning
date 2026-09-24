# AGENTS.md — `docs/`

Documents for consumers of the package.

| Document | What it is | Kept in step with |
|---|---|---|
| `MultiProjectBuildTimestamps.md` | Why `BuildTimestamp` is captured at property evaluation, and the alternatives considered | `BuildTimestamp` in `Build.props`, and the `-p:BuildTimestamp` override in `Build.ps1.template` and `Build.sh.template` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **`MultiProjectBuildTimestamps.md` is packed.** `Package.nuspec` places it at `docs/` in the package, and `README.md` links to it | Renaming or moving it breaks the README's link on nuget.org unless `Package.nuspec` and `README.md` change with it | `Package.nuspec` |
| Its relative links, `../README.md` and the `../Build.*.template` files, point at files the package also carries at its root | They are resolved against the package's own layout on nuget.org, not only against the repository | `Package.nuspec` |
| A new document here is not packed unless it is added to `Package.nuspec` | The nuspec lists its files one by one; nothing under `docs/` is included by pattern | `Package.nuspec` |
| `AGENTS.md` and `CLAUDE.md` here are not packed | They are for changing this repository, not for consumers | `Package.nuspec` |
