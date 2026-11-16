# Work state

## Status

| | |
|---|---|
| Latest release | `v2026.4.1031` — the sync to Bennewitz.Ninja.Templates `2026.4.1031`: the packed README links absolutely and the analyzer assembly carries its symbols. nuget.org lists it. The one before, `v2026.3.928`, was the `BNAV` rename |
| `main` | At `v2026.4.1031` plus the syncs to Bennewitz.Ninja.Templates `2026.4.1103`, `2026.4.1111`, `2026.4.1113` and `2026.4.1114`, and the catch-up with the bbpkg template of 2026-11-11 (JanusMael/Bennewitz.Ninja.AutoVersioning#10). **The sync to `2026.4.1114`** (2026-11-16, on the maintainer's word, from the Templates session): `Bennewitz.Ninja.CodeQuality` is raised from `2026.4.1111` to `2026.4.1113`, whose `BNCQ1009` and `BNCQ3001` now follow a delegate held in a local to its body; the restore resolves `2026.4.1113`, and the analyzer loads into both compilations and reports nothing. `docs/publishing.md` takes the template's version-floor sentences under "Choosing the version" and its two floor rows in the failure table. The scripts did not change in that release, and `scripts/repo-conventions.cs` and `scripts/release-target.cs` still match the template by SHA-256. Measured as CI runs it: the build with warnings as errors, 0 warnings, 55 of 55 tests, pack, `assert-packages`, and `check` conforms. **The sync to `2026.4.1113`** (2026-11-13, on the maintainer's word, from the Templates session): `scripts/repo-conventions.cs` and `scripts/release-target.cs` are that release's, byte for byte. `resolve` now refuses a version below the newest nuget.org lists for the id, unless nuget.org already lists it (a re-run); tried against nuget.org, whose newest is `2026.4.1031`: `v2026.4.1113` resolves, `v2026.4.1031` passes as a re-run, `v2026.4.1030` and `v2026.3.702` are refused with the floor message. `check` now also asks for the `Bennewitz.Ninja.CodeQuality` pin, which is raised from `2026.4.1104` to `2026.4.1111`, every rule at its default, the family's decision of 2026-11-10: the analyzer loads into both compilations here and reports nothing. The new floor is an invariant row in `AGENTS.md` and is named in `scripts/AGENTS.md` and `release.yml`. Measured as CI runs it: the build with warnings as errors, 0 warnings, 55 of 55 tests, pack, `assert-packages`, and `check` conforms. The sync to `2026.4.1111` brought the checks that `packages.push` resolves through `scripts/release-target.cs` and that `Bennewitz.Ninja.AssemblyQuality` is pinned. The catch-up brought `scripts/release-target.cs` and the template's `release.yml`, `docs/publishing.md`, and the `Packaging` and `AssemblyQuality` tests ported to MSTest; `NUGET_USER` is a repository variable (`JanusMael`), not a secret, so a 401 shows the name it tried, and the old secret is deleted. The first blank-version preflight, run `43050490199`, logged in and published nothing. `Microsoft.VisualStudio.Threading.Analyzers` 18.12.1 is referenced privately with only `VSTHRD101` and `VSTHRD110` on, the family's decision of 2026-11-03. Nothing the package ships changes, so no release follows from any of these syncs |
| Plans | `plans/` exists, with its own `AGENTS.md` and `CLAUDE.md` and a row in `AGENTS.md`'s Layout table. `scripts/repo-conventions.cs` counts untracked files, so those two are needed the moment the directory exists on disk, before anything in it is committed |
| CI | green |

Releases are cut by pushing a `vYEAR.QUARTER.MMDD` tag; `release.yml` resolves it through
`scripts/release-target.cs`, which refuses any other shape, a quarter that is not the date's, and a
`<family>/v` tag, since this repository declares no families. It then builds, tests, packs, asserts
the packed ids against `packages.push` / `packages.local`, publishes via NuGet Trusted Publishing
(OIDC), and creates the GitHub release. The push and the release assets name each resolved id; no
`*.nupkg` glob is pushed, which is step 1 of `plans/00009` in Bennewitz.Ninja.Templates, and
`PackagingTests` holds the workflow to it. A dispatch with `version` blank is a credential preflight
that lists those ids and publishes nothing. No long-lived NuGet API key exists in the repo: the
`NUGET_USER` variable plus the nuget.org trusted-publishing policy is the whole credential story.
`docs/publishing.md` is the runbook.

`--generate-notes` produces an empty body for this repo, because commits land directly on `main`
rather than through PRs. Release notes are written by hand after the workflow runs.

One release per calendar day. A package version is the three-part `YYYY.Q.MMDD`, so a same-day
second release has nowhere to go, and a four-part tag is not the answer: the fourth segment already
means `HHmm` in `AutoVersion`, the stamp the generator puts on an assembly, so `2026.3.925.1` reads
as a build at 00:01. Decided family-wide through Bennewitz.Ninja.Templates, relayed by the
AssemblyQuality session after it hit the question on a same-day crash fix. `AutoPackageVersion`
already computes the three-part form, so nothing here changed.

## `plans/00001` — a Debug build holds its stamp to the day

[`plans/00001`](plans/00001-a-debug-build-holds-its-stamp-to-the-day.md), approved 2026-11-16 and
frozen. Drift from it is recorded here, never edited into it.

| Step | State | Notes |
|---|---|---|
| 1 · `IncrementalBuildTests` and its fixture | **done**, red as the plan requires | Below |
| 2 · `MinimumValueOfHHmm` to `0` | **done** | Below |
| 3 · The hold in `Build.props` | **done**; the suite is green, 72 of 72 | Below |
| 4 · `BNAV05` and the validating condition | **done**; 89 of 89 | Below |
| 5 · `VersionDerivationTests` | **done**; 97 of 97, and proven red both ways | Below |
| 6 · The documents | not started | |
| 7 · The `AGENTS.md` invariants | not started | |
| 8 · A release | the maintainer's, not started | |

### Step 1, as built

`Tests/fixtures/incremental` is the four-project consumer the plan measured, `Core` ← `Model` ←
`App` with `Leaf` referencing `Model` and `Core`. `Tests/…/Incremental/FixtureConsumer.cs` copies it
to the platform's temp location, renames the templates, writes the two files NuGet would import, and
reads each project's `CoreCompile` decision out of a detailed log;
`Tests/…/Incremental/IncrementalBuildTests.cs` holds the assertions. 66 tests now, 62 green and the
four the plan says must be red until step 3.

**Red, measured 2026-11-16 against today's `Build.props`**, which is the defect the plan exists for:

| A Debug build after | Compiled |
|---|---|
| nothing changed, as a solution | `App`, `Core`, `Leaf`, `Model` — 4 of 4 |
| a body edit in `Core`, which the other three reference | 4 of 4 |
| a body edit in `Leaf`, which nothing references | 4 of 4 |
| nothing changed, one project with no `-c` | `Core`, `Leaf`, `Model` — 3 of 3 |

**`Configuration` as the package props see it** is now asserted rather than probed, five rows, and it
is what decision 2 rests on. The distinction is the solution, not the verb:

| Command, no `-c` | `Configuration` at that point |
|---|---|
| `dotnet build` of one project | **empty** |
| `dotnet build` of a solution | `Debug` |
| `dotnet pack`, either | `Release` |
| `dotnet publish` of one project | `Release` |

⚠ **Reading `Configuration` from the project body instead reports `Debug` for every one of those.**
It is the SDK's default, already applied, and a probe placed there measures nothing about this
decision. One was written that way during the plan's step-back and appeared to refute decision 2.

**Two choices worth keeping:**

- **The fixture's project files carry a `.template` suffix**, renamed on copy. As `.csproj` they are
  found by `scripts/repo-conventions.cs`, which would hold them to the family's build properties:
  measured, `check` reports "2 projects evaluated" with the suffix and would have had six without it.
  The repository already uses the suffix for files MSBuild must not pick up.
- **No `Directory.Build.props` or `Directory.Build.targets` is committed in the fixture**; the test
  writes both into the copy, because they need the absolute path of this repository's `Build.props`,
  `Build.targets` and the generator assembly the test run built.

**Drift from the plan, one item.** Step 1 says the test records what `Configuration` reaches the
package props for `dotnet test` as well. It does not: `dotnet test` over a fixture with no test
project exits 1 before reporting, and giving the fixture a test project of its own would pull MSTest
and a restore into it for one data point. The four commands that can be measured are. `dotnet test`
builds Debug, so decision 2 covers it either way; nothing rests on the gap.

**`--filter-class` is not an MTP argument.** It reports zero tests run with exit code 5, the same way
`--nologo` does, so the suite runs whole.

### Steps 2 and 3, as built

**Step 2**, the floor. `Constants.MinimumValueOfHHmm` is `0`, not `100`, and its comment no longer
claims `0100` is the minimum valid `HHmm`. The midnight hour runs `0` to `59`; what rejects a
revision that is not a time is the hour and minute range check below it, `60` to `99` included, and
what rejects the unstamped `1.0.0.0` is `MinimumValueOfMMdd`, untouched. `BuildVersionFromFileTests`
gained four accepting rows, red first against the old constant — `…429.0` as 00:00, `…429.1` as
00:01, `…429.59` as 00:59, and a three-part `2026.4.1116` as that day at midnight, which pins
decision 7's accepted consequence — and two rejecting rows, `…429.60` and `…429.99`, which passed
before and after and are there as regression guards.

**Step 3**, the hold: the two lines `plans/00001` specifies, in place of the one that set
`BuildTimestamp`. Debug and empty take `DateTime.Today`; everything else keeps `DateTime.Now`.

**Measured on this machine, the suite green at 72 of 72** — the drafting session's figures
reproduced independently, by a harness in the repository rather than by scratch scripts:

| A Debug build after | Before step 3 | After |
|---|---|---|
| nothing changed, as a solution | 4 of 4 | **0 of 4** |
| a body edit in `Core`, which the other three reference | 4 of 4 | **1 of 4** |
| a body edit in `Leaf`, which nothing references | 4 of 4 | **1 of 4** |
| nothing changed, one project with no `-c` | 3 of 3 | **0 of 3** |
| an unchanged **Release** build | 4 of 4 | 4 of 4, deliberately |

Confirmed in the generated editorconfig directly: all four projects carry
`build_property.BuildTimestamp = 20261116000000`, one value, the day.

⚠ **The ordinary skip line is not the one that reads like it.** MSBuild writes
`Skipping target "CoreCompile" because all output files are up-to-date…` when a target is skipped,
and `Target "CoreCompile" skipped. Previously built successfully.` only for a second request inside
one build. Worse, **only a target that actually runs gets a `Target "CoreCompile" in file … from
project "X"` header**, so a parser that takes the project from that header cannot attribute a skip at
all. `ReadCompileDecisions` therefore tracks the project from any line naming one and matches both
skip forms. The first version matched one form and took the project from the header: it read 1 of 4
projects as having reached a decision, and the plan's guard — every project must reach a decision a
test can read — failed the run instead of reporting that nothing compiled. That guard earned its
place on the first green build it saw.

The incremental tests spawn real builds, two per test, so the suite now runs about 90 seconds rather
than 3. Node reuse is off and `MSBUILDDISABLENODEREUSE` is set, because a reused node can hold a
stale evaluation of the very file under test.

### Step 4, as built

`BNAV05`, an Error, in `AssemblyInfoGenerator`, with its row in `AnalyzerReleases.Unshipped.md` and
in the README's Diagnostics table. `Build.props` validates the stamp **in the derivation's own
condition** — fourteen digits by regex, month 1 to 12, day no more than
`$([System.DateTime]::DaysInMonth(year, month))`, hour, minute and second in range — so a stamp it
cannot validate leaves `AutoVersion` and `AutoPackageVersion` **empty rather than wrong**. A target
reports it. 17 new tests in `Incremental/StampValidationTests.cs`; the suite is 89 of 89.

**Measured, after:** `20261345000000`, `20260230000000`, `20260229000000`, `20260431000000`,
`20260105250000`, `20260105006000` and `2026010500050X` all derive nothing, where before they derived
`2026.4.1345`, `2026.1.230` and so on. `20240229000000` derives `2024.1.229` and `20260229000000`
derives nothing, which pins `DaysInMonth` against the hardcoded 31 someone will reach for;
`20260131000000` derives `2026.1.131`. Every valid stamp in the step 5 table is unchanged.

⛔ **The two halves divide the work; they do not both fire.** The reporting target runs before the
compiler, so left ungated it failed the build first, every time, and `BNAV05` **could never be
reached in a consumer build** — measured, eight malformed stamps, all reported by MSBuild, none by
the generator. The target is therefore gated on `GenerateAutoVersionedAssemblyInfo != 'true'`:

| | Reports | Why |
|---|---|---|
| Generator enabled | `BNAV05` only | Precise, documented, suppressible as a rule, and it names the offending value |
| Generator disabled | the `Build.props` error only | No generator runs, so nothing else can report — and this is the project that packs `$(AutoPackageVersion)` |

Both leave the two properties empty either way, and `dotnet msbuild -getProperty:AutoPackageVersion`
answers empty rather than wrong in both, which is the assertion that the validation lives in the
condition and not only in the target.

⛔ **Neither message may name the other's id.** The target's error first read "…the generator reports
the same thing as BNAV05 where it is enabled", which put the string `BNAV05` in the log of a build
where no generator ran. The test asserting that the ungated case does *not* report `BNAV05` failed on
it, and a consumer grepping the output would have been misled the same way. The target's error also
carries **no `Code`**: `BNAV05` is the analyzer rule, tracked in `AnalyzerReleases` and suppressible
as one, and an MSBuild error sharing the id would be neither.

**What steps 5 to 7 should build on**, since it was written for step 4 and the plan predates it:

- `FixtureConsumer.Evaluate(property, args)` runs `dotnet msbuild -getProperty` over the fixture's
  `Leaf` project and returns the value — an evaluation, no target, no compile, so it is fast and is
  the right tool for step 5's stamp table. `FixtureConsumer.Run(verb, target, args)` is the build
  form, and `Build(args)` the solution shorthand.
- The fixture's written `Directory.Build.props` reports `FIXTURE-CONFIGURATION`, `FIXTURE-STAMP`,
  `FIXTURE-AUTOVERSION` and `FIXTURE-AUTOPACKAGEVERSION` from a target, read back with
  `FixtureConsumer.ReadReportedValue(log, key)`, which throws rather than returning empty when the
  report is missing.
- Step 5's end-to-end case needs a built assembly's `AssemblyFileVersion`: the fixture's output sits
  under `<copy>/src/<Project>/obj/<Configuration>/net10.0/<Project>.dll`, and
  `BuildVersionFromFileTests` already shows the `FileVersionInfo` pattern.

**Drift from the plan.** Step 4 says a fixture build "reports `BNAV05` and the `Build.props` error"
for each bad stamp. It reports exactly one of the two, by the division above, which the step could
not have anticipated: that the target pre-empts the compiler is only visible once both exist. The
plan's other step-4 assertions — the ungated case, `-getProperty` answering empty, `DaysInMonth`
pinned by the two Februaries — hold as written.

### Step 5, as built

`Tests/…/Incremental/VersionDerivationTests.cs`, the guard for the one invariant this repository had
no automated check for: the CalVer derivation exists twice, in `BuildVersion` and in `Build.props`,
and one `BuildTimestamp` goes into both. 8 new tests; the suite is 97 of 97.

**Each row is asserted three ways, not two.** The MSBuild value against the C# value catches an
ordinary one-sided change; both against a literal written down when the plan was approved catches a
change made to *both halves at once*, which the first comparison cannot see by construction. The
stamp is parsed in the test exactly as `AssemblyInfoGenerator` parses it — `yyyyMMddHHmmss`,
`InvariantCulture`, `AssumeLocal` — since parsed any other way it would not be the comparison it
claims to be.

⭐ **Proven red both ways before being trusted**, which is what the plan asks of this step and the
only evidence that a guard guards anything:

| Perturbation, one half only | Result |
|---|---|
| `Build.props`' Q1 boundary from `<= 3` to `<= 2` | **1 failure**, "the last minute of Q1", on the C#-against-MSBuild assertion |
| `GetQuarter` from `(Month + 2) / 3` to `(Month + 1) / 3` | **8 failures** — three rows here, plus the five `BuildVersionTests` culture rows |

Both were reverted and the tree re-measured green. Note the asymmetry: the C# half already had
independent cover, and the MSBuild half had **none** — one perturbation of `Build.props` would have
shipped silently before this step, and that is the defect the step exists for.

**The end-to-end case** builds the fixture with `20260913161300` and compares MSBuild's `AutoVersion`
with the `AssemblyFileVersion` the generator wrote into `Leaf.dll` in that same build, read back with
`FileVersionInfo` from the Win32 version resource the compiler synthesised — the four integer parts,
not the `FileVersion` string, whose form belongs to the resource. Nothing in it is compared against a
value a test author typed: each side computed its own from the one stamp.

**`FixtureConsumer.EvaluateMany` is new**, and `Evaluate` now delegates to it. Starting `dotnet` is
nearly all an evaluation costs, and `-getProperty` may be repeated — MSBuild answers them together
under one `Properties` object — so a row wanting `AutoVersion` and `AutoPackageVersion` asks once.
**Measured: 3m44s for the suite with two calls a row, 2m32s with one.** The property name is anchored
to its opening quote in the pattern, so asking for `AutoVersion` never reads `AutoPackageVersion`,
which ends with the same characters.

⚠ **The suite's wall time is not stable and should not be quoted as one number.** The same green tree
measured 2m32s and 5m57s on this machine within the hour, and the two perturbation runs took 4m49s
and 7m24s. Every incremental test spawns real builds, so the figure tracks whatever else the machine
is doing. What is stable is the *shape*: roughly 3 seconds of the suite is unit tests and the rest is
MSBuild.

**No drift from the plan.** Step 5's table, its three-way assertions, the end-to-end case and the
red-both-ways requirement all hold as written.

⛔ **Found while building step 5, and it belongs to steps 6 and 7: `Tests/AGENTS.md` is stale.** Its
file table lists neither `Incremental/IncrementalBuildTests.cs`, `Incremental/StampValidationTests.cs`
nor `Incremental/VersionDerivationTests.cs`, and its closing sentence — *"Nothing here runs
`AssemblyInfoGenerator` through a generator driver or evaluates `Build.props` / `Build.targets`;
those are exercised only by building a consuming project"* — has been wrong since step 1. Building a
consuming project is now exactly what those three do. Steps 1 and 4 each left this behind; step 5 is
naming it rather than making the same omission a third time.

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

The ids in this section are the ones every published version emits. Read `BNAV0n` for the same
signals in logs from any build against `main`, or against whatever release first carries the rename.

### `BuildVersion.TryGetFromFile` is correct but unreachable by consumers

The intent is for consumers to call it from their own tests to read the stamp back off a built
binary. They cannot. `Package.nuspec` ships the assembly to `analyzers\dotnet\cs` only, with
`developmentDependency: true` and `IncludeBuildOutput=false`, so the compiler loads it and it never
enters the consumer's reference set. There is no `lib/`. Confirmed by search: nothing outside this
repo calls `TryGetFromFile` or references `Bennewitz.Ninja.AutoVersioning.SourceGenerators`.

**Decided 2026-11-16, by the maintainer: a separate `.Abstractions` package**, the first row below.
It wants a plan of its own and has none yet; `plans/00001` scopes it out deliberately, fixing only
what `TryGetFromFile` computes, not what reaches a consumer.

⛔ **One correction to carry into that plan.** The preference list below used to say the
`.Abstractions` option needs "its own nuget.org trusted-publishing policy". It does not, and acting
on that would break the release: nuget.org mints **one API key per token exchange, scoped to one
matching policy**, so two policies matching this owner, repository and workflow would publish the
first id permanently and reject the rest `403` in the same `dotnet nuget push`. A second id needs
the **existing** policy's Glob Patterns widened to cover it — the field is plural for this reason —
plus a row in `packages.push`, which `scripts/assert-packages.cs` then requires and
`scripts/release-target.cs` resolves.

The correctness fix landed without closing this deliberately. Three ways to close it, in preference
order:

| Option | Trade-off |
|---|---|
| **A separate `.Abstractions` package with a real `lib/netstandard2.0` — chosen** | Cleanest. Keeps the analyzer a pure `developmentDependency` and ships no Roslyn. Most work. Needs the one policy's glob widened and a `packages.push` row, not a policy of its own |
| Add `lib\netstandard2.0` to this package | One nuspec line, but publishes the whole generator assembly as a runtime reference, exposing Roslyn-dependent types that fail at runtime because `SuppressDependenciesWhenPacking` strips the dependency |
| Document a `HintPath` straight at the analyzer DLL | No packaging change, but the path carries the version number and it is not a supported reference model |

## Constraints worth keeping

- **The diagnostic ids are `BNAV00`–`04`, and the prefix does not change again.** The family scheme
  is `BN` + the product's initials with the rule number kept — `BNAQ`, `BNXQ`, `BNCQ` — four letters
  because an analyzer's id shares one flat namespace with every analyzer a project loads, where a
  two-letter prefix collides and a seventeen-letter one helps nobody. The scheme and its reasoning
  are Bennewitz.Ninja.AssemblyQuality's `## Decisions`, which is the family's source for rule ids;
  neither `docs/repository-conventions.md` nor `scripts/repo-conventions.cs` says anything about
  them. `BAUTOVERSIONING00`–`04` shipped up to `2026.3.916`; `BNAV00`–`04` from `2026.3.928`, with
  the numbers unchanged. That rename is the family's **second** exception to "a prefix never changes
  once one id in it has shipped", and a far larger one than the first — AssemblyQuality took `AQ` →
  `BNAQ` with one published version used only as a test dependency, against nine public versions
  here. The maintainer took it knowingly on 2026-09-28: nothing consumes the ids by name, and
  `TreatWarningsAsErrors`, the only consumer behaviour that depends on these diagnostics, does not
  name ids. Take a new number next time, never a new prefix.
- **The history has two roots, and `v2026.2.429` sits on the abandoned one.** `326b1f6` ("Initial
  commit", 2026-04-29) is parentless, and everything `main` reaches starts there; the earlier root
  `d257c5e` carries the lineage the `v2026.2.429` tag points into, which is *not* an ancestor of
  `main`. So `git tag --contains <commit>` omits `v2026.2.429` for anything on `main`, and
  `git log --follow` reports files added at `326b1f6` that existed before it. Archaeology across that
  boundary has to read the tagged trees directly — `git show <tag>:<path>` — which is how the
  `AnalyzerReleases` rollover established that `BAUTOVERSIONING00`–`03` shipped in `2026.2.429` and
  `04` in `2026.2.521`. The nine tags do all correspond to published nuget.org versions.
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
