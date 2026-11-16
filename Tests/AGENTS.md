# AGENTS.md — `Tests/`

One project, `Bennewitz.Ninja.AutoVersioning.Tests`: MSTest on Microsoft.Testing.Platform
(`EnableMSTestRunner`, an `Exe`, and `test.runner` in the root `global.json`), net10.0. It
references the generator project as an ordinary library and calls its classes directly.

| File | What it guards |
|---|---|
| `AssemblyInfoTemplateTests.cs` | The exact text and the character set `AssemblyInfoTemplate` emits: printable ASCII plus exactly one U+2665 |
| `BuildVersionTests.cs` | `BuildVersion` is identical under de-DE, th-TH, ar-SA, fa-IR and ja-JP, and `ToString()` is safe to embed |
| `BuildVersionFromFileTests.cs` | `BuildVersion.TryGetFromFile` over real emitted assemblies with chosen file versions |
| `Packaging/PackagingTests.cs` | Every packable project is in `packages.push` or `packages.local`, its id read where pack reads it (the root project's from `Package.nuspec`); no id is in both; `release.yml` globs nothing, resolves through `release-target.cs`, still pushes and releases, and keeps its credential preflight. Ported from the bbpkg template |
| `Architecture/AssemblyQualityTests.cs` | AssemblyQuality's BNAQ1001, BNAQ1002, BNAQ1004 and BNAQ1005 over the compiled generator, each skipping nothing and, where it can, inspecting something; BNAQ1005: every compiled grant names an assembly the two grant files at the root allow. BNAQ1002 inspects nothing until the generator references a namespace to keep inside it. Ported from the bbpkg template |
| `Incremental/FixtureConsumer.cs` | Not a test: the harness the three below share. Copies `fixtures/incremental` outside the repository, renames the `.template` suffixes away, writes the two files NuGet would import, and runs or evaluates it. `Run` / `Build` build it; `Evaluate` and `EvaluateMany` read properties with `-getProperty`, which runs no target; `ReadCompileDecisions` reads each project's `CoreCompile` decision out of a detailed log; `ReadReportedValue` reads a `FIXTURE-*` report, throwing rather than returning empty when it is missing |
| `Incremental/IncrementalBuildTests.cs` | That a Debug build holds `BuildTimestamp` to its day and so compiles nothing when nothing changed, counted as MSBuild's own `CoreCompile` decisions; and what `Configuration` reaches the package props for each verb without `-c` |
| `Incremental/StampValidationTests.cs` | That a stamp which does not parse is reported rather than replaced: `BNAV05` where the generator runs, the `Build.props` error where it does not, never both, and `-getProperty` answering empty rather than wrong |
| `Incremental/VersionDerivationTests.cs` | That the CalVer derivation in `Build.props` and the one in `BuildVersion` agree, over a table of stamps and once end to end against a built assembly's `AssemblyFileVersion` |

⛔ **The `Incremental/` tests build a real consuming project**, which is the only way to exercise
`Build.props` and `Build.targets` at all: what they assert lives in the order MSBuild evaluates files
in, and no unit test can see that. They spawn `dotnet` repeatedly, so they are most of the suite's
wall time — roughly three seconds of it is everything else. Nothing here still runs
`AssemblyInfoGenerator` through a Roslyn generator driver; the fixture's own compiler loads it, as a
consumer's would.

## Rules

| Rule | Why |
|---|---|
| `dotnet test` is never passed `--nologo`, nor `--filter-class` | Neither is a Microsoft.Testing.Platform argument. Each reports **zero tests run with exit code 5**, which reads as a suite that found nothing rather than as a rejected argument. There is no narrowing flag to reach for: run the suite whole |
| This directory stays in `DefaultItemExcludes` in the root csproj | The root project's glob would otherwise compile these sources into the netstandard2.0 analyzer |
| `Microsoft.CodeAnalysis.CSharp` is referenced here directly | The generator references it with `PrivateAssets="all"`, so it does not flow |
| A test that sets `Thread.CurrentThread.CurrentCulture` restores it in `finally`, in a `[DoNotParallelize]` class like `BuildVersionTests` | A culture left set leaks into whichever test that thread runs next |
| U+2665 is written as the escape `♥`, never as a literal | The test file stays pure ASCII, so it cannot be broken by the mishap it catches |
| An emitted probe assembly calls `CreateDefaultWin32Resources` and uses the stream overload of `Emit` | `Compilation.Emit(path)` writes no Win32 version resource, so `FileVersionInfo` reads nothing |
| ⛔ **Never weaken a packaging test to make it pass** | `PackagingTests` reads the project files, the nuspec and both package lists; when it fails, the list or the project is wrong, not the test |
