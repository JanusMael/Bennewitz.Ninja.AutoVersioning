# AGENTS.md — `Tests/`

One project, `Bennewitz.Ninja.AutoVersioning.Tests`: MSTest on Microsoft.Testing.Platform
(`EnableMSTestRunner`, an `Exe`, and `test.runner` in the root `global.json`), net10.0. It
references the generator project as an ordinary library and calls its classes directly.

| File | What it guards |
|---|---|
| `AssemblyInfoTemplateTests.cs` | The exact text and the character set `AssemblyInfoTemplate` emits: printable ASCII plus exactly one U+2665 |
| `BuildVersionTests.cs` | `BuildVersion` is identical under de-DE, th-TH, ar-SA, fa-IR and ja-JP, and `ToString()` is safe to embed |
| `BuildVersionFromFileTests.cs` | `BuildVersion.TryGetFromFile` over real emitted assemblies with chosen file versions |

Nothing here runs `AssemblyInfoGenerator` through a generator driver or evaluates `Build.props` /
`Build.targets`; those are exercised only by building a consuming project.

## Rules

| Rule | Why |
|---|---|
| `dotnet test` is never passed `--nologo` | Microsoft.Testing.Platform rejects it and reports zero tests run, with exit code 5 |
| This directory stays in `DefaultItemExcludes` in the root csproj | The root project's glob would otherwise compile these sources into the netstandard2.0 analyzer |
| `Microsoft.CodeAnalysis.CSharp` is referenced here directly | The generator references it with `PrivateAssets="all"`, so it does not flow |
| A test that sets `Thread.CurrentThread.CurrentCulture` restores it in `finally`, in a `[DoNotParallelize]` class like `BuildVersionTests` | A culture left set leaks into whichever test that thread runs next |
| U+2665 is written as the escape `♥`, never as a literal | The test file stays pure ASCII, so it cannot be broken by the mishap it catches |
| An emitted probe assembly calls `CreateDefaultWin32Resources` and uses the stream overload of `Emit` | `Compilation.Emit(path)` writes no Win32 version resource, so `FileVersionInfo` reads nothing |
