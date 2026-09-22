# Work state

## Status

| | |
|---|---|
| Latest release | `v2026.3.916` — published to NuGet.org, verified present in the CDN |
| `main` | clean, in sync with `origin/main`, tagged at the release |
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
  different cause. Two things that look like causes and are not: the `AddSource` calls already pass
  `Encoding.UTF8`, and the source files are UTF-8 without a BOM, which Roslyn reads correctly.
