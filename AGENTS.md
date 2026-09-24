# AGENTS.md — Bennewitz.Ninja.AutoVersioning

> For anyone changing this repository, human or agent. The invariants a change must not break, the
> commands, and the checklists for recurring work. Each top-level directory has an `AGENTS.md` of
> its own. Work state is [`PROGRESS.md`](PROGRESS.md). The family's conventions are prescribed in
> [`docs/repository-conventions.md`](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md)
> in Bennewitz.Ninja.Templates.

## What this repository is

The NuGet package `Bennewitz.Ninja.AutoVersioning`: a Roslyn incremental source generator,
`AssemblyInfoGenerator`, that stamps a consumer's assembly attributes with a CalVer version
(`YEAR.QUARTER.MMdd.HHmm`) and emits `DirectoryBuildInfo.BuildRelease`. It ships as a single
netstandard2.0 analyzer assembly plus two MSBuild files NuGet imports into the consuming project.
[`README.md`](README.md) is the consumer's documentation and, packed, the nuget.org description;
the constraints learned the hard way are in `PROGRESS.md` under "Constraints worth keeping".

This repository was not generated from the `bbpkg` template. The project sits at the root and the
layout is flat: one top-level directory per group of generator source.

## Layout

| Path | What it holds |
|---|---|
| `Bennewitz.Ninja.AutoVersioning.csproj` | The generator project: netstandard2.0, `IsRoslynComponent`, assembly `Bennewitz.Ninja.AutoVersioning.SourceGenerators`, packed through `Package.nuspec` |
| `AssemblyInfoGenerator.cs` | The `IIncrementalGenerator`: reads `build_property.*`, reports `BAUTOVERSIONING00`–`04`, adds `AutoVersionedAssemblyInfo.g.cs` and `DirectoryBuildInfo.g.cs` |
| `Build.props` | Shipped to consumers as `Bennewitz.Ninja.AutoVersioning.props`: the `CompilerVisibleProperty` items, `BuildTimestamp`, `AutoVersion` / `AutoPackageVersion`, and the setup-reminder target. Not imported by this repository's own build |
| `Build.targets` | Shipped as `Bennewitz.Ninja.AutoVersioning.targets`: turns off the SDK attributes the generator emits |
| `Package.nuspec` | What the package contains, file by file |
| `Pack.ps1` | Interactive local build and pack of Debug and Release into `packages/` |
| `BannedSymbols.txt` | Roslyn APIs banned by `RS0030`; see `Syntax/AGENTS.md` |
| `AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md` | Analyzer release tracking for the `BAUTOVERSIONING` diagnostics |
| `Directory.Build.props.template`, `Build.ps1.template`, `Build.sh.template` | Files for consumers to copy, packed at the package root so the README's links resolve |
| `global.json` | Opts `dotnet test` into Microsoft.Testing.Platform |
| `Versioning/` | `BuildVersion`, the CalVer algorithm, and its comparer |
| `Syntax/` | The Roslyn syntax builders behind `AssemblyInfoTemplate` |
| `Enums/` | Reflection helpers over enum types, used by `AssemblyInfoSyntaxFactory.Identity.Flags` |
| `Hashing/` | `HashCodeUtility` and a vendored `xxHash32` |
| `Tests/` | `Bennewitz.Ninja.AutoVersioning.Tests`, MSTest on Microsoft.Testing.Platform |
| `scripts/` | `repo-conventions.cs`, a copy of the family's conventions check |
| `docs/` | `MultiProjectBuildTimestamps.md`, packed into the package |
| `.github/` | CI, the release workflow, and `repository.json` |

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| The project sits at the root, so its default `**/*.cs` glob would compile every C# file beneath it. `DefaultItemExcludes` keeps `Tests\**` and `scripts\**` out. A new top-level directory holding C# that is not generator source is added there | Test sources compile into the netstandard2.0 analyzer and fail it; a file-based app's `#!` line fails the build with `CS9314` | `DefaultItemExcludes` in `Bennewitz.Ninja.AutoVersioning.csproj`; CI's `build` job |
| The package carries one assembly, in `analyzers\dotnet\cs`, and no dependencies (`IncludeBuildOutput` false, `SuppressDependenciesWhenPacking`). Code on the generator's path uses only what the compiler host supplies: Roslyn and the BCL | A type from a NuGet dependency is missing in the consumer's compiler, and the generator fails there, never here | `Package.nuspec`; nothing automated |
| The SDK attribute suppressions stay in `Build.targets`; `BuildTimestamp` and the `AutoVersion` derivation stay in `Build.props`, ungated | A consumer enabling the generator in its own `.csproj` gets `CS0579` duplicate attributes and an empty `BuildTimestamp` | Comments in both files; nothing automated |
| Every attribute `AssemblyInfoTemplate` emits that the SDK also generates has its `GenerateAssembly*Attribute` set to false in `Build.targets` | `CS0579` in every consumer | Nothing automated |
| Each `CompilerVisibleProperty` in `Build.props` matches a `build_property.*` key read in `AssemblyInfoGenerator.Initialize` | The generator reads the property as absent, silently | Nothing automated |
| `Build.props` and `Build.targets` are packed under the package id, into both `build\` and `buildMultiTargeting\` | NuGet never imports them, or a multi-targeting consumer misses them, and the generator reports `BAUTOVERSIONING00` although enabled | `Package.nuspec` |
| The `AutoVersion` derivation in `Build.props` computes what `BuildVersion.CalculateAbsoluteVersion`, `GetQuarter` and `GetBuildInfo` compute | A package's version disagrees with the assembly stamped inside it | The warning in `Build.props`; nothing automated |
| Every `DiagnosticDescriptor` id in `AssemblyInfoGenerator` has a row in `AnalyzerReleases.Shipped.md` or `AnalyzerReleases.Unshipped.md` | Release tracking loses the rule's history | `RS2000` / `RS2001` from Microsoft.CodeAnalysis.Analyzers, reading the `AdditionalFiles` in the csproj |
| Consumer files keep their `.template` suffix and stay in `Package.nuspec` | As `Directory.Build.props`, MSBuild would import it into this repository's own build; unpacked, the README's links break on nuget.org | `Package.nuspec` |
| `README.md`, `Directory.Build.props.template` and the `BAUTOVERSIONING04` message describe the same setup | A consumer follows one and gets another | Nothing automated |

## Commands

```bash
dotnet build -c Release --nologo
dotnet test Tests/Bennewitz.Ninja.AutoVersioning.Tests/Bennewitz.Ninja.AutoVersioning.Tests.csproj -c Release
dotnet pack -c Release --no-build --nologo -p:Version=<version> --output ./packages/Release
dotnet run --file scripts/repo-conventions.cs -- check
```

- These are the steps `ci.yml` and `release.yml` run. The workflows spell the property `/p:Version`,
  which Git Bash on Windows rewrites into a path; `-p:` is the portable form.
- `dotnet test` must not be passed `--nologo`: Microsoft.Testing.Platform rejects it and reports
  zero tests run.
- A script run from this root passes `--file`; without it, `dotnet run` binds to the root project.
- `pwsh -NoProfile -File Pack.ps1` prompts for a version and packs Debug and Release locally.

## Checklists

**Releasing:** CI green on the commit to release. Move the rows of `AnalyzerReleases.Unshipped.md`
into `AnalyzerReleases.Shipped.md` under `## Release <version>`. Tag `vYYYY.Q.MMDD` and push;
`release.yml` takes the version from the tag. A version on nuget.org can never be replaced, so tag
only with the maintainer's go-ahead. Then confirm the version on nuget.org, not only a green run.

**Adding a diagnostic:** a `DiagnosticDescriptor` in `AssemblyInfoGenerator.cs` with the next
`BAUTOVERSIONINGnn` id, a row in `AnalyzerReleases.Unshipped.md`, and a row in the README's
Diagnostics table. A multi-line message keeps its first line self-contained; `RS1032` is off for
that reason.

**Adding an MSBuild property the generator reads:** a `CompilerVisibleProperty` in `Build.props`, a
`build_property.<Name>` read in `AssemblyInfoGenerator.Initialize`, and a row in the README's
Configuration Reference. A property consumers must set also goes into `Directory.Build.props.template`,
the `BAUTOVERSIONING04` message and the reminder target in `Build.props`.

**Adding a top-level directory:** give it an `AGENTS.md` and a `CLAUDE.md` containing `@AGENTS.md`,
or exempt it in `.github/repository.json` under `undocumented` with the reason. If it holds C# that
is not generator source, add it to `DefaultItemExcludes` in the csproj.

**Every change:** update `PROGRESS.md` in the same commit. Commits are Conventional Commits.
