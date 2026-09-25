# Work state

## Status

| | |
|---|---|
| Latest release | `v2026.3.916` — published to NuGet.org, verified present in the CDN |
| `main` | Past the tag by build and CI changes, none needing a release on its own. `f093331`: the family's standard build properties (`plans/00004` in Bennewitz.Ninja.Templates) through a root `Directory.Build.props`, and every package version in `Directory.Packages.props`; the packed nupkg keeps the same 11 entries and no dependencies, and the analyzer assembly's `AssemblyCompany` becomes `Bennewitz.Ninja`. `2631253`, and again for the `nuget` topic rule (Templates `fb6961a`): `scripts/repo-conventions.cs` is the template's current copy |
| CI | green |

Releases are cut by pushing a `vYEAR.QUARTER.MMDD` tag; `release.yml` builds, packs, publishes via
NuGet Trusted Publishing (OIDC), and creates the GitHub release. No long-lived NuGet API key exists
in the repo — `NUGET_USER` plus the nuget.org trusted-publishing policy is the whole credential story.

`--generate-notes` produces an empty body for this repo, because commits land directly on `main`
rather than through PRs. Release notes are written by hand after the workflow runs.

## Open

### Consumer reports `BAUTOVERSIONING` "not configured" errors, seen only in CI

Not reproduced. Left open pending logs from the reporting repo.

**Ruled out** — do not re-investigate these without new evidence:

| Hypothesis | Result |
|---|---|
| Multi-project / solution build triggers it | Three-project solution, clean `obj`, builds clean |
| The reporter's exact 3-level `Directory.Build.props` chain | Reproduced verbatim (L1 root + L2 scoped + L3 with the `GetPathOfFileAbove` import); builds clean |
| Trailing `..\` backslash in an L3 import breaks on Linux | Tested on Linux via WSL; MSBuild normalises it, chain resolves |
| Platform difference generally | Windows and Linux both clean across all three levels |
| `GenerateAssemblyInfo=false` breaking their `InternalsVisibleTo` | Their attribute comes from a `<Compile Include>`'d `.cs` file, which that property cannot affect; verified present |

**The deduction that narrows it:** a broken props chain produces *silence*, not errors. If a
`Directory.Build.props` fails to chain upward, the project also loses the `PackageReference`, so the
analyzer never loads and emits nothing. For `BAUTOVERSIONING02/03` to fire, a project must have all
of: the analyzer, **and** `GenerateAutoVersionedAssemblyInfo=true`, **but** empty
`AssemblyCompany`/`AssemblyProduct`. Since those three are set in the same `PropertyGroup` in the
reporting repo's root file, they cannot arrive separately by inheritance — so something downstream
clears them, or supplies the flag by a different route.

**Diagnostic** — evaluates the chain without building:

```bash
dotnet msbuild <project>.csproj -getProperty:GenerateAutoVersionedAssemblyInfo -getProperty:AssemblyCompany -getProperty:AssemblyProduct
```

Flag `true` with empty company/product is the culprit signature. All three empty means the chain
broke, which contradicts seeing diagnostics at all and would itself be informative.

**Diagnostic IDs matter in the logs.** `TreatWarningsAsErrors` in the reporting repo promotes
`BAUTOVERSIONING00`/`04` from warnings to hard errors, which looks identical to a real failure.
`00`/`04` means the flag never arrived; `02`/`03` means it arrived but company/product did not.

### `BuildVersion.TryGetFromFile` is correct but unreachable by consumers

The intent is for consumers to call it from their own tests to read the stamp back off a built
binary. They cannot. `Package.nuspec` ships the assembly to `analyzers\dotnet\cs` only, with
`developmentDependency: true` and `IncludeBuildOutput=false`, so the compiler loads it and it never
enters the consumer's reference set. There is no `lib/`. Confirmed by search: nothing outside this
repo calls `TryGetFromFile` or references `Bennewitz.Ninja.AutoVersioning.SourceGenerators`.

The correctness fix landed without closing this deliberately. Three ways to close it, in preference
order:

| Option | Trade-off |
|---|---|
| A separate `.Abstractions` package with a real `lib/netstandard2.0` | Cleanest. Keeps the analyzer a pure `developmentDependency` and ships no Roslyn. Most work |
| Add `lib\netstandard2.0` to this package | One nuspec line, but publishes the whole generator assembly as a runtime reference, exposing Roslyn-dependent types that fail at runtime because `SuppressDependenciesWhenPacking` strips the dependency |
| Document a `HintPath` straight at the analyzer DLL | No packaging change, but the path carries the version number and it is not a supported reference model |

## Constraints worth keeping

- **SDK attribute suppressions belong in `Build.targets`, never `Build.props`.** A `.props` is
  imported before the consuming project body, so gating on `GenerateAutoVersionedAssemblyInfo` there
  fails for projects that enable the generator in their own `.csproj` — symptom is seven `CS0579`
  duplicate attribute errors plus an empty `BuildTimestamp`. `BuildTimestamp` and the `AutoVersion`
  derivation must stay in `Build.props` and stay ungated, so they remain reachable from the project
  body for `$(AutoPackageVersion)`.
- **The analyzer ships as a single self-contained DLL.** The nuspec packs one assembly and
  `SuppressDependenciesWhenPacking` strips dependencies, so any NuGet reference the generator code
  actually uses would be absent at runtime in consuming projects. This is why `xxHash32` is vendored
  rather than taken from `System.IO.Hashing`, which drags `System.Buffers` and `System.Memory` on
  netstandard2.0.
- **`BannedSymbols.txt` entries need full documentation-comment IDs.** Parameterless methods omit
  the parens, generic method arity takes a double backtick, and generic methods require the
  `~ReturnType` suffix. Derive them from Roslyn's `DocumentationCommentId` rather than by hand; a
  malformed entry matches nothing and fails silently.
- **`Versioning/BuildVersion.cs` and the MSBuild version derivation in `Build.props` are two
  expressions of the same CalVer algorithm** and must not drift.
- **The generator emits printable ASCII (U+0020 to U+007E) plus exactly one deliberate exception,
  U+2665.** The build name is `Built with ♥ {commitHash}`, or `Built with ♥` when no hash was
  supplied; the heart is branding, not corruption. Measured on a real consumer build, it is stored
  as UTF-16 in the Win32 `ProductVersion` resource and round-trips intact, with
  `FileVersionInfo.GetVersionInfo(...).ProductVersion` comparing byte-identical to the runtime
  `AssemblyInformationalVersion` attribute — so a consumer seeing those two disagree is looking at a
  different cause. `AssemblyInfoTemplateTests` allowlists ASCII plus U+2665 and pins the exact text,
  so any *other* character above U+007E fails the build. Two things that look like causes and are
  not: the `AddSource` calls already pass `Encoding.UTF8`, and the source files are UTF-8 without a
  BOM, which Roslyn reads correctly.
- **`BuildVersion` must never consult the ambient culture.** `GetBuildInfo` derives the Build and
  Revision version parts from `ToString("MMdd")`/`ToString("HHmm")`, which pick the *culture's
  calendar*. Measured before the fix: a 29 April timestamp produced `2026.2.1112.845` under `ar-SA`
  (Umm al-Qura) and `2026.2.209.845` under `fa-IR` (Solar Hijri) — a wrong `AssemblyVersion` and
  `AssemblyFileVersion`, not merely a cosmetic string, and a locale with non-Latin digits would have
  thrown in `Convert.ToUInt16`. The rendered build time is invariant for the same reason, since it is
  interpolated raw into `DirectoryBuildInfo.BuildRelease`, where a locale-supplied quote would break
  the generated source. `BuildVersionTests` runs every assertion under de-DE, th-TH, ar-SA, fa-IR and
  ja-JP and asserts the invariant result, so it stays correct whatever a given ICU version decides
  those cultures mean.
- **The test project needs the `test.runner` opt-in in `global.json`, and `dotnet test` must not be
  passed `--nologo`.** MSTest 4.x runs on Microsoft.Testing.Platform; the .NET 10 SDK refuses to
  drive MTP through the legacy VSTest target, and `dotnet test` forwards unrecognised flags to the
  test host, which rejects them and reports zero tests run with exit code 5 rather than failing
  loudly. Removing `global.json` reinstates the VSTest error.
- **The CalVer stamp is packed into integers, so decompose it arithmetically, never through a
  string.** `MMdd` and `HHmm` lose their leading zero once stored as `Version` parts: 08:45 is
  stamped as `0845` and read back as `845`. `TryGetFromFile` used to feed that to
  `DateTime.TryParseExact` against the four-character `"HHmm"` format and discard the returned
  `bool`, so **every build between 01:00 and 09:59 silently read back as midnight** — on any
  machine, not just an exotic locale. The same round-trip let the ambient culture pick the digits,
  and an out-of-range value reached the `DateTimeOffset` constructor and threw
  `ArgumentOutOfRangeException` straight out of a `Try` method (month 13, 30 February). It is now
  `buildNumber / 100` and `buildNumber % 100` with explicit range validation, and
  `TryGetFromFile` validates before constructing so an unrecognised version returns false rather
  than throwing. `BuildVersionFromFileTests` emits real assemblies carrying chosen file versions —
  `Compilation.Emit(path)` writes no Win32 version resource, so the probe must call
  `CreateDefaultWin32Resources` and use the stream overload, or `FileVersionInfo` reads nothing.
- **The generator's `.csproj` sits at the repository root**, so its default `**/*.cs` glob would
  compile `Tests/` into the netstandard2.0 analyzer assembly. `DefaultItemExcludes` holds it back.
  Keeping the test project in a subdirectory also leaves `dotnet build` at the root unambiguous,
  which is what both workflows invoke.
