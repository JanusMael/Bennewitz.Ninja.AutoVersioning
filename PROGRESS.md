# Work state

## Status

| | |
|---|---|
| Latest release | `v2026.4.1116` — `plans/00001`: the Debug day-hold, and `BNAV05`, **a breaking change**. **nuget.org lists it, confirmed 2026-11-17.** Do not read the green workflow as the release: the push logged `Created` and `Your package was pushed` at 15:47 CST on 2026-11-16, and a probe 17 minutes later still read `404` from the flat container, the package page and the registration index — NuGet's validation had not finished, and a failed validation is indistinguishable from a slow one to anyone but the owner. The one before, `v2026.4.1031`, was the sync to Bennewitz.Ninja.Templates `2026.4.1031`; `v2026.3.928` was the `BNAV` rename |
| `main` | **At `v2026.4.1116`, plus two documentation commits, `d5549c6` and `5216670`** — the stamp-spread paragraph of `docs/MultiProjectBuildTimestamps.md`, settled after it was wrong three times; nothing the package ships changes, so no release follows. `v2026.4.1116` itself is `plans/00001`. **Before that release** came the syncs to Bennewitz.Ninja.Templates `2026.4.1103`, `2026.4.1111`, `2026.4.1113` and `2026.4.1114`, and the catch-up with the bbpkg template of 2026-11-11 (JanusMael/Bennewitz.Ninja.AutoVersioning#10). **The sync to `2026.4.1114`** (2026-11-16, on the maintainer's word, from the Templates session): `Bennewitz.Ninja.CodeQuality` is raised from `2026.4.1111` to `2026.4.1113`, whose `BNCQ1009` and `BNCQ3001` now follow a delegate held in a local to its body; the restore resolves `2026.4.1113`, and the analyzer loads into both compilations and reports nothing. `docs/publishing.md` takes the template's version-floor sentences under "Choosing the version" and its two floor rows in the failure table. The scripts did not change in that release, and `scripts/repo-conventions.cs` and `scripts/release-target.cs` still match the template by SHA-256. Measured as CI runs it: the build with warnings as errors, 0 warnings, 55 of 55 tests, pack, `assert-packages`, and `check` conforms. **The sync to `2026.4.1113`** (2026-11-13, on the maintainer's word, from the Templates session): `scripts/repo-conventions.cs` and `scripts/release-target.cs` are that release's, byte for byte. `resolve` now refuses a version below the newest nuget.org lists for the id, unless nuget.org already lists it (a re-run); tried against nuget.org, whose newest is `2026.4.1031`: `v2026.4.1113` resolves, `v2026.4.1031` passes as a re-run, `v2026.4.1030` and `v2026.3.702` are refused with the floor message. `check` now also asks for the `Bennewitz.Ninja.CodeQuality` pin, which is raised from `2026.4.1104` to `2026.4.1111`, every rule at its default, the family's decision of 2026-11-10: the analyzer loads into both compilations here and reports nothing. The new floor is an invariant row in `AGENTS.md` and is named in `scripts/AGENTS.md` and `release.yml`. Measured as CI runs it: the build with warnings as errors, 0 warnings, 55 of 55 tests, pack, `assert-packages`, and `check` conforms. The sync to `2026.4.1111` brought the checks that `packages.push` resolves through `scripts/release-target.cs` and that `Bennewitz.Ninja.AssemblyQuality` is pinned. The catch-up brought `scripts/release-target.cs` and the template's `release.yml`, `docs/publishing.md`, and the `Packaging` and `AssemblyQuality` tests ported to MSTest; `NUGET_USER` is a repository variable (`JanusMael`), not a secret, so a 401 shows the name it tried, and the old secret is deleted. The first blank-version preflight, run `43050490199`, logged in and published nothing. `Microsoft.VisualStudio.Threading.Analyzers` 18.12.1 is referenced privately with only `VSTHRD101` and `VSTHRD110` on, the family's decision of 2026-11-03. Nothing the package ships changes, so no release follows from any of these syncs |
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

⛔ **Release notes are written by hand, and `--generate-notes` is not even a starting point to edit:
it reports merges, not effects.** It has now misled twice. For `v2026.4.1031` it listed two pull
requests from before that release; for `v2026.4.1116` it listed five template syncs and **never
mentioned `BNAV05`, a breaking change, or the Debug hold** — a body that stayed live for a day
before being replaced by hand. It yields an *empty* body only when nothing landed by pull request:
work done in this session lands directly on `main`, but the Templates peer session opens pull
requests here (#9 through #13), which is exactly what it found and reported as the release.

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
| 6 · The documents | **done**; `check` and `assert-packages` both pass | Below |
| 7 · The `AGENTS.md` invariants | **done**; `check` conforms | Below |
| 8 · A release | **done** — `v2026.4.1116`, tagged by the maintainer 2026-11-16, listed on nuget.org 2026-11-17 | Below |

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

### Step 6, as built

Decisions 8 and 9, in five files. `check` conforms, `assert-packages` reports *"Each README of
`packages.push` links only absolutely"*, and the suite stays 97 of 97.

| File | What changed |
|---|---|
| `docs/MultiProjectBuildTimestamps.md` | The consistency claim corrected to what was measured; a *Why Debug holds its stamp to the day* section with the 4-of-4 → 0-of-4 table and a *What the hold costs*; two new rows in *Alternatives considered* — the hold itself, and the dismissed stamp-that-stays-still |
| `README.md` | The `BuildTimestamp` and `Configuration` rows of *Configuration Reference*; *Timestamp Consistency* gains the hold, the measurement and the `BuildRelease` cost |
| `Build.props` | The comment above `BuildTimestamp`, which described the old per-build capture, replaced with why there are two lines, why an empty `Configuration` counts as `Debug`, and what the hold costs |
| `Build.ps1.template`, `Build.sh.template` | Decision 9: each says a stamp passed there **overrides** the hold, so a consumer running them in the inner loop knows to drop the line |

**The claim that was wrong, and exactly how.** The document said that captured at evaluation "all
projects normally land on the same value". Measured, 19 of 24 clock-stamped builds stamped their
projects with **more than one value, up to three**.

⛔ **The correction itself was then wrong, and shipped that way in `v2026.4.1116`.** It added that
"none of those 24 crossed a minute, so the *version* — precise only to `HHmm` — agreed throughout",
and called a stronger reading an overstatement. **Re-measured 2026-11-17 on SDK 11.0.100.** The
method, which needs no harness of its own: lay out `Tests/fixtures/incremental` as
`FixtureConsumer.Create` does, add a `BeforeTargets="CoreCompile"` target that echoes
`$(MSBuildProjectName)` with `$(BuildTimestamp)`, then 24 times delete every `obj` and `bin` and run
`dotnet build incremental.slnx -c Release -v:detailed -nodeReuse:false -m:1`, counting the distinct
stamps each build reported. Release, because Debug now holds to the day and would report one value by
construction.

⛔ **And the re-measurement's first conclusion was wrong too — caught by stepping back on it before
anything else was built on it.** The whole phenomenon needs **`-m:1`**, which is what
`FixtureConsumer.Run` hardcodes for log attributability and what **no consumer passes**:

| How the build ran | Distinct stamps across the four projects |
|---|---|
| `dotnet build <solution>`, default parallelism | **1 value, 16 of 16** — 10 plain, plus 6 with a 4-second `Exec` delay inserted into `Core`, which the other three reference |
| `dotnet publish <project>`, with or without `-m:1` | 1 value, 6 of 6 |
| `dotnet build <solution> -m:1` | **2 or 3 values, 24 of 24**, ~1–2 s apart; **1 of 24 crossed a minute** (`20261117112859` / `20261117112900`) |

All four projects reported in every run — checked, so a single value is agreement and not a missing
report. Under `-m:1` the late one is always `Leaf`, built last because it references the other two,
evaluating after `Core` has built. Under default parallelism MSBuild evaluates the solution's
projects together *before* any of them builds, which the inserted delay could not perturb.

📏 **The lesson is the shape of the claim, three times over.** Each edition of that paragraph
generalised a measurement whose configuration it never named: the original said projects always
agree; `v2026.4.1116` said 19 of 24 disagree and the version still agrees; the first fix here said
24 of 24 disagree and the version sometimes does not. The common defect is the conclusion, not the
arithmetic — and the last of the three was written in the same commit that diagnosed the defect in
the other two. The document now reports each configuration with its number, draws no general
conclusion, and states plainly what is **not** measured: a genuinely large solution, where a
parallel build may still reach projects minutes apart.

### The large solution, measured — 2026-11-17

⛔ **Measured on the maintainer's word, and it overturned the `-m:1` conclusion above.** The spread
is not an artifact of a flag: it **scales with project count**, and four projects was simply too few
to show it.

150 projects (one root, 149 leaves referencing it), generated in temp, `dotnet build -c Release`
with **default parallelism** and no flags, SDK 11.0.100. All 150 reported in all 25 builds, so a
single value would be agreement and not a missing line:

| | |
|---|---|
| builds with more than one stamp value | **25 of 25** |
| most distinct values in one build | **12** |
| spread, seconds | min 1, max 11, **mean 8** |
| builds whose projects took two different `HHmm` minutes | **2 of 25** — observed, not modelled |

The two crossings are `202611171401`/`202611171402` and `202611171402`/`202611171403`: **two
assemblies of one build carrying versions a minute apart.** That is the risk the document had been
reaching for since its first edition, and the edition before this one had ruled it out.

**The mechanism, now consistent with every measurement taken.** A project's stamp is captured when
the build first *evaluates* it, so the spread is how long evaluation takes to reach the last project.
At four projects a parallel build evaluates them together before any compiles — a 4-second `Exec`
delay forced into the one the others reference could not separate them over 6 builds. `-m:1`
separates them only because it serialises evaluation behind each project's build, which is a small-
scale stand-in for what scale does on its own. At 150 projects no flag is needed.

**Consequences, stated separately because they differ:** `BuildRelease` carries the seconds, so it
differs between projects on essentially every clock-stamped build of a large solution.
`AutoVersion` (`HHmm`) and `AutoPackageVersion` (the day) differ only when the spread straddles a
boundary — 2 of 25 here.

⚠ **Still not measured, and said so in the document rather than generalised past:** projects slow to
*compile*. These are one-type projects, so what was measured is the duration of evaluation.

📏 **The guidance this finally produces**, and the reason the measurement was worth taking: in a
solution of any real size, pass `-p:BuildTimestamp` on the builds whose version must be uniform —
Release and CI, where the clock is still taken per project and the artifact leaves the machine.
`Build.ps1.template` and `Build.sh.template` already do. Debug needs nothing, being held to its day.

⚠ **`assert-packages` reads every `.nupkg` in the directory it is given, so a stale one fails it
misleadingly.** Run against `packages/Release`, it reported four relative links in "the README" —
all four from a `2026.3.928` package left there in September, whose README genuinely did link
relatively before `v2026.4.1031` fixed it. The cited line numbers do not match the current README,
which is the tell. Pack into a fresh directory before believing a failure: `packages/` is gitignored,
so whatever is in it survives everything else.

**No drift from the plan.** Step 6's three verifications hold as written, including that `README.md`,
`Directory.Build.props.template` and the `BNAV04` message still describe one setup — three properties
required and three optional, unchanged, there being nothing new for a consumer to set.

### Step 7, as built

The invariants, in four `AGENTS.md` files. `check` conforms.

| File | What changed |
|---|---|
| `AGENTS.md` | Two new invariant rows — the Debug hold, guarded by `IncrementalBuildTests`; and *a stamp that is set and does not parse is reported, never replaced in silence*, guarded by `StampValidationTests`. **The two-derivations row's guard stops being "nothing automated" and becomes `VersionDerivationTests`**, which is what `plans/00001` decision 11 exists for. The ungated-`Build.props` row now names the test that covers its case |
| `Versioning/AGENTS.md` | The same guard on its own copy of the two-derivations rule; a new row for `MinimumValueOfHHmm` being `0`; and a *What reading a stamp back now answers* section carrying decision 7's two consequences |
| `Tests/AGENTS.md` | The four `Incremental/` files, which it had never listed, and the correction below |
| — | `PROGRESS.md`, this section |

⛔ **`Tests/AGENTS.md` had been wrong since step 1, and steps 1 and 4 both missed it.** Its closing
sentence read *"Nothing here runs `AssemblyInfoGenerator` through a generator driver or evaluates
`Build.props` / `Build.targets`; those are exercised only by building a consuming project"* — while
three new test classes were doing exactly that. The file table listed none of them either. A
directory's `AGENTS.md` is what the next session reads before touching that directory, so a stale one
costs more than a stale comment: this plan's own steps are the evidence.

**The `--filter-class` trap is now a rule there**, beside `--nologo`: neither is an MTP argument, both
report zero tests run with exit code 5, and that reads as a suite that found nothing rather than as a
rejected argument.

⭐ **Correction, measured 2026-11-16: `--filter` *is* supported, and the belief recorded in step 1 —
"so the suite runs whole" — was wrong.** `dotnet test … --filter "FullyQualifiedName~VersionDerivationTests"`
runs those 8 in 53 seconds, and `~BuildVersionTests` runs 15 in half a second, where the whole suite
takes minutes. One failing argument (`--filter-class`) was generalised into "there is no narrowing
flag", and that generalisation cost every run since step 1 the full suite. `Tests/AGENTS.md` now
carries both halves: `--filter` for the inner loop, the whole suite for CI and for any claim that it
is green.

**Four `nothing automated` rows remain in the root `AGENTS.md`, and eleven across the other
directories.** They are left as they are deliberately — the step asks that the table "name a real
guard for every row it can", and these guard things no test in this repository reaches: the packed
assembly's dependency surface, the `GenerateAssembly*Attribute` suppression set, the
`CompilerVisibleProperty`-to-`build_property` correspondence, and that three documents describe one
setup. Each is a candidate for a plan of its own, none for a silent edit here.

**No drift from the plan.** Step 7's verification — `check` conforms, and the invariant table names a
real guard for every row it can — holds as written.

### Step 8, as released — `v2026.4.1116`

Prepared 2026-11-16 by decision (everything but the tag), tagged the same day by the maintainer,
confirmed on nuget.org 2026-11-17.

- **`BNAV05` moved** from `AnalyzerReleases.Unshipped.md` into `AnalyzerReleases.Shipped.md` under
  `## Release 2026.4.1116`. `Unshipped.md` now holds only its header, which is the state
  `RS2000`/`RS2001` expect between releases. Proven to be a real check, not a skipped one: removing
  the row produced `error RS2000: Rule 'BNAV05' is not part of any analyzer release`.
- **The notes were written by hand** and replaced the body `--generate-notes` had produced. See the
  finding below: that body was live and misleading for a day.
- **The packed artifact was verified from the release asset**, not from the working tree — unzipped,
  and read: `build/Bennewitz.Ninja.AutoVersioning.props` carries `DateTime]::Today` (the hold); the
  analyzer assembly carries `BNAV00` through `BNAV05`; the corrected
  `docs/MultiProjectBuildTimestamps.md`, the README's hold section and both templates' override
  warning are all present.

⛔ **A green `release.yml` is not a release, and 17 minutes of `404` is not a failure either.** The
push logged `Created … 429ms` and `Your package was pushed.`; the flat container, the package page
and the registration index all still read `404` / `"upper":"2026.4.1031"` 17 minutes later, and read
`200` with `2026.4.1116` listed the next morning. NuGet's validation sits between the two, and
**anonymously a validation that failed looks exactly like one that has not finished** — only the
owner's nuget.org account or email distinguishes them. `AGENTS.md`'s Releasing checklist already
said to confirm on nuget.org and not on a green run; this is what that sentence is worth.

⛔ **`--generate-notes` produced a misleading body, and it stayed live for a day.** It listed the
five template-sync pull requests and **never mentioned `BNAV05`, a breaking change, or the Debug
hold** — a release whose public notes described chores. It is the second time it has misled here (the
previous release's body named two pull requests from before that release). The body is now the
hand-written notes. ⛔ **The `--generate-notes` body is not a starting point to edit; it is wrong
about what the release contains, because it reports merges rather than effects.**

⛔ **The `## Release 2026.4.1116` heading was the tag's date, so that commit committed to tagging on
2026-11-16.** Tag on another day and both the heading and the tag must change together — the version
*is* the date, and `scripts/release-target.cs` refuses a quarter that is not its date's. The
Releasing checklist in `AGENTS.md` says so, beside the one-release-per-day rule.

**The one-release-per-day rule is now in the Releasing checklist**, on the maintainer's word of
2026-11-16. It had been recorded here but deliberately withheld from `AGENTS.md`, having arrived as
another session's relay rather than from the maintainer; that reservation is now resolved. The
checklist is where someone cutting a release actually looks, which is the whole point of moving it.

### Decisions taken 2026-11-16, after steps 5 to 7

| Question | Decision |
|---|---|
| Route for the six commits | **Straight to `main`**, matching steps 1 to 3 of this plan and this file's own statement that commits land directly. Pushed `3377252..1c814a9`; CI green, run `43954588140` |
| How much of step 8 | **Everything but the tag**, as above |
| The one-release-per-day rule in the checklist | **Add it** |
| `.Abstractions` | **Write the plan now** — `plans/00002`, draft |
| The 15 `nothing automated` invariant rows | **Leave them, revisit per change.** They are honestly labelled, and several guard what no test here reaches — a consumer's compiler, the packed assembly's load surface. A guard is added when a change touches that area, not as a campaign |
| The tag | **`v2026.4.1116`, today, by the maintainer.** The staged heading already matches |
| `.Abstractions`' public surface | **Trim the unused three before the first release** — `plans/00002` decision 8 |
| The stale worktree | **Removed**, with its branch |

### Decisions taken 2026-11-17

| Question | Decision |
|---|---|
| `plans/00002`'s namespace for the moved types — **permanent after publication** | **`Bennewitz.Ninja.AutoVersioning`**, matching the package family a consumer installs, rather than leaving `…SourceGenerators` (which would have a consumer write `using …SourceGenerators;` to read a version number out of a package called `.Abstractions`) or `…AutoVersioning.Versioning` (a segment implying a sub-area of three types). Mechanical, and it lands in step 1 beside the API trim so step 2's move stays a pure move. `plans/00002` decision 11 |
| `.Abstractions`' README | **Its own**, not shared. The two packages have different audiences, and a shared README would open the library's nuget.org page with `CompilerVisibleProperty`, `Directory.Build.props` and the `BNAV` diagnostics. A shared README was not available as a middle path anyway: it needs a declared family in `.github/repository.json`, which also changes the release tag shape to `<family>/v…`. `plans/00002` decision 12 |
| Whether to measure a large parallel solution | **Measure it**, rather than leave the document's opening premise resting on nothing. ⛔ **It overturned the previous correction.** See *The large solution, measured* below |
| `.Abstractions`' target framework | **netstandard2.0**, not the family's net10.0. Exempted in `.github/repository.json` with its reason. Reach is the point of the package; multi-targeting was offered and declined. See *Step 2, as built* |
| The trusted-publishing policy's Glob Patterns | **Widened by the maintainer to the wildcard `Bennewitz.Ninja.AutoVersioning*`**, 2026-11-19, which covers both ids. ⛔ Step 6's widening is therefore **done** — and it was the one gate nothing could verify, since a preflight's token exchange happens before any id is considered |
| The wildcard's latent hazard | **A tripwire test, not a narrower policy.** `packages.local` must stay empty while the policy is a wildcard, and `PackagingTests.While_the_policy_is_a_wildcard_no_id_is_private` fails the build if an id appears there. Proven to fire |
| `FuzzyBuildVersionComparer` | **Restored as `public` in `.Abstractions`**, with the hashing tooling kept `internal`. Reverses part of decision 8; see *Drift from decision 8* below |
| `Hashing/`, left with no compiling caller by step 1 | **Resolved by the above rather than decided**: it moved into `.Abstractions`, and the restored comparer is its caller again |

⛔ **Verified while settling the README decision: a package carrying NO README passes the link check
silently.** `scripts/assert-packages.cs` guards it with `r.Readme is not null`, so omitting
`<readme>` from the new nuspec yields a bare nuget.org page and a green run. `plans/00002` step 3
now has to confirm the packed README exists, not merely that its links are absolute.

### Two findings from checking those decisions rather than assuming them

⛔ **`Architecture/AssemblyQualityTests.cs` is a trap for `plans/00002`.** It reads
`private static readonly Assembly Shipped = typeof(BuildVersion).Assembly;`. Today `BuildVersion`
lives in the generator, so that is the generator. **Move `Versioning/` into `.Abstractions` and the
whole class silently retargets to the new assembly — and no test fails.** The generator, which is
the thing a consumer's compiler loads and the only reason those rules exist here, would stop being
tested while the suite stayed green. `plans/00002` decision 9 carries the fix.

📏 **Three public members of `Versioning/` are used nowhere**, measured 2026-11-16:
`BuildVersion.Generate(UInt16, UInt16)` has no caller in the repository at all;
`FuzzyBuildVersionComparer` appears only in one comment in `Hashing/HashCodeUtility.cs`; and
`GetQuarter` is called once, from `BuildVersion.cs` itself. The nested `Constants`, `Formats` and
`VersionBuildInfo` are already `private`. `GetQuarter` is the one that matters most: a public
extension method on `DateTimeOffset` would appear on **every `DateTimeOffset` in every consuming
project** the moment the package ships.

**The `.claude/worktrees/distracted-tharp-4a7182` worktree is gone**, with its branch — 69 MB, last
commit `3688ce9` of 2026-09-22, and **nothing on it that was not already on `main`**. It held a full
stale copy of the source, an older `AssemblyInfoGenerator.cs` among it, and a search run without
pruning it returned those copies looking exactly like real matches. `worktrees` belongs in the prune
list of any search run here.

## `plans/00002` — `TryGetFromFile` reaches a consumer

[`plans/00002`](plans/00002-tryget-from-file-reaches-a-consumer.md), **approved 2026-11-17 and
frozen** (`bbfa418`, committed alone). Drift from it is recorded here, never edited into it. Its two
maintainer-only questions were settled the same day and are in the decisions table above.

| Step | State | Notes |
|---|---|---|
| 0 · Read the policy's Glob Patterns | **done** — and it needs widening | Below |
| 1 · The API trim and the namespace | **done** | Below |
| 2 · The new project and the move | **done** | Below |
| 3 · Packaging | **done** | Below |
| 4 · A consumer test over the `.nupkg` | **done**; 111 of 111, and proven red | Below |
| 5 · The documents | **done** 2026-11-23 | Below |
| 6 · Glob Patterns widened, and a preflight | **widening done** 2026-11-19; preflight not yet run | Below |
| 7 · A release | not started | |

### Step 6, half done — the policy is widened

⛔ **The maintainer widened the policy to `Bennewitz.Ninja.AutoVersioning*` on 2026-11-19.** That
wildcard covers both ids, so the coverage gap step 0 found is closed — **and it was the one gate in
this plan that nothing could verify.** `docs/publishing.md`: *"A green preflight proves the login
works, not that the policy covers every id. The exchange succeeds before any package id is
considered, so nothing in a preflight can detect a coverage gap. Only the push can, and by then part
of the release is permanent."*

⚠ **A wildcard carries a hazard the repository had already written down.** `packages.local` states
that the policy's pattern must match **nothing** in that file, and a wildcard over the shared prefix
matches any private id by construction — so the first one added would be published permanently by
the next release. That is the `Bennewitz.Ninja.DiffView` near-miss that file exists to prevent.

📏 **The maintainer chose a tripwire over narrowing the policy.**
`PackagingTests.While_the_policy_is_a_wildcard_no_id_is_private` fails the build the moment
`packages.local` stops being empty, naming the wildcard, the consequence and the remedy. Proven to
fire by adding an id and watching it fail. `packages.local`'s own comment now says the same.
⛔ **If it ever fails, narrow the policy — do not delete the test.**

**Not done:** the blank-`version` dispatch preflight. It cannot detect coverage, but it does prove
the login still works and prints the ids a tag would publish, now two.

### Step 0, as read — the pattern does not cover the second id

⛔ **The policy's *Glob Patterns and Packages* field reads `Bennewitz.Ninja.AutoVersioning`, a bare
exact id with no wildcard, so it does NOT match `Bennewitz.Ninja.AutoVersioning.Abstractions`.**
Step 6's widening is mandatory, not a formality. Reported by the maintainer 2026-11-17 from
<https://www.nuget.org/account/trustedpublishing>.

**This does not invalidate the plan.** The pattern is a field the policy owner edits; what step 0
existed to catch was the opposite case — a policy that could not be made to cover both ids at all.

📏 **Two exact patterns, not a wildcard, is the form to use.** The field is plural and carries as
many patterns as needed, so `Bennewitz.Ninja.AutoVersioning` and
`Bennewitz.Ninja.AutoVersioning.Abstractions` both go in it. ⛔ A wildcard
(`Bennewitz.Ninja.AutoVersioning*`) would also match a future id — and `packages.local`'s own
comment requires that the policy's pattern match **nothing** in that file. `packages.local` is empty
today, so a wildcard is harmless today and a trap the first time a local-feed-only id is added, at
which point it would be published permanently on the next release. Exactness here matches the
repository's existing refusal to push a `*.nupkg` glob.

⚠ **Still unread, and needed before step 6:** the same policy's **Scopes**. `.Abstractions` is a
*new* id, so the policy must carry **Publish new packages**, not only *Publish new versions of
existing packages*. `docs/publishing.md` already states this; nobody has confirmed the live value.

### Drift from decision 8 — `FuzzyBuildVersionComparer` is restored, and public

⛔ **The maintainer reversed part of `plans/00002` decision 8 on 2026-11-19.** The plan is approved
and frozen, so it is **not edited**; this is the record.

Decision 8 had three unused public members trimmed before the id was published, and step 1 deleted
the comparer under it. Asked why, the reasoning held — unused, and the only thing in `Versioning/`
depending on the rest of the generator — but the maintainer's call is that the type is **wanted as
public API**, with the hashing tooling behind it kept `internal`.

| Member | Decision 8 | Now |
|---|---|---|
| `DateTimeOffsetExtensions.GetQuarter` | `internal` | `internal`, unchanged |
| `BuildVersion.Generate(UInt16, UInt16)` | `internal` | `internal`, unchanged |
| `FuzzyBuildVersionComparer` | deleted | **restored, `public`** — a supported API of `.Abstractions` |
| `HashCodeUtility` | not in the decision | **`public` → `internal`** |

**`Hashing/` moved to `Abstractions/Hashing/`** and was renamespaced to
`Bennewitz.Ninja.AutoVersioning`, the same treatment `Versioning/` got, with the generator linking
its sources back. That is what lets the public comparer keep its `GetHashCode` while the machinery
stays unpublished. ⭐ It also dissolves the dead-code finding step 1 created: `Hashing/` has a
compiling caller again, so the open question about deleting it is closed by not arising.

⚠ **It carries no dependency with it.** `Hashing/` uses only BCL types — the one mention of
`HashCode.Combine` is in a comment — so `.Abstractions` still packs with an **empty dependency
group**. Checked before the move, not after.

📏 **A public API with no test is a defect here, so it has ten now.** The comparer's rule row read
*"Guarded by: Nothing automated"*, which was tolerable for unused internal code and is not for
something a consumer can depend on. `FuzzyBuildVersionComparerTests` asserts the two subtleties that
had only ever been **described** in its remarks:

- **The `HHmm` revision is not a linear scale.** 09:59 and 10:00 are one minute apart while the
  integers 959 and 1000 are 41 apart. **Proven red** by returning the revision undecoded: exactly
  one test failed, `TheRevisionIsDecodedFromHHmm_NotComparedAsAnInteger`.
- **The relation is deliberately not transitive.** With a 3-minute tolerance 10:00≡10:03 and
  10:03≡10:06 but 10:00≢10:06 — asserted, so the warning against deduplicating by sorting is a test
  rather than a sentence.

### Step 5, as built — the documents

| Document | What changed |
|---|---|
| `README.md` | A **Reading a stamp back** section after *Version Format*: the companion id, a `PackageReference`, the five-line call, and that neither package requires the other. It is the packed nuget.org description, so every link in it is absolute |
| `Abstractions/README.md` | A **Comparing two stamps** section. `FuzzyBuildVersionComparer` became public API on 2026-11-19, *after* this README was written, so the package's own page documented a surface it did not have. ⚠ The non-transitivity is stated there as a consumer-facing warning, not as a remark: a tolerance equality that a reader deduplicates with is the way this type gets misused |
| `Abstractions/AGENTS.md` | Its path table listed `Versioning/` alone and never gained `Hashing/`, which moved in on 2026-11-19. Both rows now say what is public and what is `internal`, and `README.md` has a row of its own |
| `Abstractions/Versioning/AGENTS.md` | Its closing paragraph still said these types *"reach no consumer yet"*, which step 2 made false and step 4 disproved. It now names `PackagedConsumerTests` as the proof — ⚠ **and says the surface stays changeable until step 7 publishes it**, which is the only remaining window |
| `PROGRESS.md` | This section; the step table; and the old finding *"`TryGetFromFile` is correct but unreachable by consumers"*, which still read as live and still described `plans/00002` as an unapproved draft with four open questions |

⚠ **No nuget.org badge for the new id yet.** The one in `README.md` is for the generator; a second
would render as *invalid* on every page that shows it until step 7 publishes the id, including the
packed description on nuget.org itself. It goes in after the release, not before.

**Verified:** `check` conforms, 111 of 111 tests pass, and `assert-packages` over a fresh pack of
both ids reports two packages, both declared, **both READMEs linking absolutely** — which is the
check that would have caught a relative link in either new section.

### Step 4, as built — a consumer really calls it

`Tests/fixtures/consumer/` is a one-project consumer, and
`Tests/…/Packaging/PackagedConsumerTests.cs` is the test the plan calls its whole point. One run:
pack `.Abstractions` into a feed, copy the fixture out of the repository, restore it against that
feed **alone**, build it with the generator loaded, run the built assembly, and compare the stamp it
read back through `BuildVersion.TryGetFromFile` with the `AutoVersion` that same build derived.

⛔ **It consumes the `.nupkg`, never the built DLL**, which is the plan's own prohibition and the
reason the test can fail at all: the defect `plans/00002` exists to fix is a *packaging* defect, so a
test wired the way `FixtureConsumer` wires the generator — `<Analyzer Include="…{the built dll}" />`
— would have proved nothing. The fixture gets a `NuGet.config` whose `<clear />` drops every source
the machine would otherwise contribute and leaves the packed directory as the only one, and a real
`<PackageReference>`.

**Proven red**, by packing with `-p:IncludeBuildOutput=false -p:NoWarn=NU5128`, which is what empties
an SDK-packed library: the consumer failed to build with `CS0246 — The type or namespace name
'BuildVersion' could not be found`, exactly the consumer-visible defect. ⭐ The restore still
*succeeded* there: the package resolved and delivered nothing, which is the shape of the failure
this whole plan is about.

⭐ **And the comparison is not vacuous**, which any two-sided equality risks. Built once with
`-p:BuildTimestamp=20260102030405`: the report read `2026.1.102.304` and the consumer read the same
value back out of the assembly's Win32 version resource, so both sides follow a stamp the test chose
rather than agreeing on whatever the clock gave.

**Three details that are load-bearing, not housekeeping:**

- ⚠ **The package version is unique per run and the restore gets its own packages folder**
  (`globalPackagesFolder` in the written `NuGet.config`). A fixed version would let NuGet's global
  cache serve an *earlier* run's package — one that still carried a `lib/` — in place of the one
  under test, and the mutation above would have stayed green.
- ⚠ **The built assembly is run directly, never through `dotnet run`.** A Release build takes a
  fresh `BuildTimestamp` every time, so a rebuild would restamp the assembly and compare two
  different builds.
- The fixture also calls `FuzzyBuildVersionComparer`, which **nothing else proves a consumer can
  reach**: its ten tests all call it from inside this repository, where it is reachable whether or
  not the package exposes it.

`FixtureConsumer`'s `RepositoryRoot`, `GeneratorAssembly` and `CopyDirectory` are `internal` rather
than private now, and `PackagingTests.Pack` takes extra arguments, so there is one way to find the
root and one way to pack rather than a second copy of each.

**Cost:** the whole `Packaging/` folder runs in **3.6 s for all nine tests**, measured 2026-11-19 —
the pack and the consumer round trip overlap. The suite is 111 of 111 in 72 s.

### Step 3, as built — packaging

Two ids now pack, both declared: `assert-packages` over a fresh directory reports
`2 packed, all declared in packages.push or packages.local` and both READMEs linking absolutely.

**`.Abstractions` packs the SDK's way, with no `NuspecFile`** — a deviation from the generator,
which needs one only because its layout is unusual. An ordinary library's default
`lib\netstandard2.0` placement is already right, and a hand-written file list is one more thing to
get wrong. The packed result is exactly three files: the nuspec, `README.md`, and
`lib/netstandard2.0/Bennewitz.Ninja.AutoVersioning.Abstractions.dll`. Its dependency group is
**empty**, so the private analyzers in `Directory.Build.props` do not leak into it.

**Its own README**, decision 12, written in this step rather than step 5 because packing needs the
file to exist. It describes a library — `TryGetFromFile`, the four integer parts, the midnight
behaviours — and links only absolutely.

#### How a second id gets packed, which step 2 left open

⛔ **Each packable project is NAMED, in `ci.yml`, `release.yml` and `Pack.ps1`.** There is no
solution file and none was added: `release.yml` **already named the root project**
(`dotnet pack Bennewitz.Ninja.AutoVersioning.csproj …`), so these workflows are repo-specific
already, not byte-identical template copies, and a second pack line is an ordinary local edit rather
than drift from Bennewitz.Ninja.Templates. `ci.yml`'s bare `dotnet pack` now names both too.

⚠ **In `release.yml` the failure mode of forgetting one is the expensive one**: the push names each
id from `packages.push`, so a declared-but-unpacked id fails the run on a missing file — *after* the
first id has published permanently. The comment at that step says so.

#### Two defects found in the guards themselves

⛔ **`PackagingTests.PackableIds()` could not see the new project at all.** It enumerated
`*.csproj` at the root and one level under `Tests/` — its own summary claimed that was "every
project this repository has", which step 2 made false. The test exists to catch an unclassified
packable project, and it was **blind** to the one just added: its "fails closed" design is defeated
by discovery, not by the flag. It now also sweeps every directory one level below the root. One
level only, never a recursive walk — a `.csproj` never lives in `bin/` or `obj/`.

📏 **The new content test was proven red two ways, and one of them taught something better.** The
plan asks for a case that goes red when `lib/netstandard2.0` is removed:

| Mutation | Result |
|---|---|
| `<IncludeBuildOutput>false</IncludeBuildOutput>` | **`dotnet pack` itself fails**, `NU5128` — *"Add lib or ref assemblies for the netstandard2.0 target framework"* — escalated to an error by `TreatWarningsAsErrors`. ⭐ So a `lib`-less package **cannot be produced here at all**, which is a stronger guarantee than the test |
| `PackageReadmeFile` and the `None Include` removed | Packs **cleanly**, and the test fails with its own message. ⛔ `assert-packages` would have passed that package in silence, its link check being guarded by `r.Readme is not null` |

The README half is therefore the half that was reachable, and it is the half that matters: a
README-less package is a blank page on nuget.org that every other gate accepts.

### Step 2, as built — the new project and the move

`Abstractions/` is a new top-level directory holding
`Bennewitz.Ninja.AutoVersioning.Abstractions.csproj` and `Versioning/`, moved there from the root
with `git mv` so the history follows. `IsPackable` is **`false`** until step 3: the id is not in
`packages.push`, and `assert-packages` fails the build on a packed id declared in neither list.

**Verified as the plan requires:** `dotnet build -c Release` 0 warnings 0 errors with
`TreatWarningsAsErrors`; **98 of 98 tests**, the suite run whole (97 as before, plus the new guard
below); the packed generator still carries `BuildVersion` in
`analyzers/dotnet/cs/…SourceGenerators.dll`, read out of the `.nupkg`; `assert-packages` still sees
exactly one id, and the package still has no `lib/`, which is step 3's job.

**Decision 9's fix is in.** `Architecture/AssemblyQualityTests.cs` read
`typeof(BuildVersion).Assembly`; it now anchors on `typeof(AssemblyInfoGenerator).Assembly` — the
generator cannot move — and a new test, `The_assembly_under_test_is_the_generator`, asserts the
simple name is `Bennewitz.Ninja.AutoVersioning.SourceGenerators`. That is the 98th test. Without it
the failure mode is silent: a rule reading the wrong assembly finds nothing and passes.

#### Four things step 2 needed that the plan does not mention

⛔ **1. There is no solution file, and CI builds the root project and the test project by path.** A
new project is therefore an orphan — `dotnet build` at the root would never compile it, and nothing
would notice it breaking. The root csproj now carries a `ProjectReference` to it with
`ReferenceOutputAssembly="false"` and `PrivateAssets="all"`: a build dependency that passes **no
assembly**, so the analyzer gains no reference and no `CS0436` from the same types arriving as both
source and reference. Confirmed by the build producing
`Abstractions/bin/Release/netstandard2.0/…Abstractions.dll`.

⚠ **How the second id gets PACKED is still open, and it is step 3's first problem.** `dotnet pack`
at the root packs the root project alone, so step 3's "two ids" verification needs either a solution
file or an explicit second pack — and `release.yml` and `ci.yml` came from the bbpkg template, so
editing them here is drift from Bennewitz.Ninja.Templates rather than a local change.

⛔ **2. A nested project's `obj\` is not excluded from the root glob.** `Abstractions\**` is now in
`DefaultItemExcludes`, and the sources are linked back explicitly with
`<Compile Include="Abstractions\Versioning\**\*.cs" />`. Without the exclusion the root project
would compile `Abstractions\obj\…AssemblyInfo.cs` as well and fail with `CS0579`.

⛔ **3. The friend-grant file is generated from every project's assembly name**, and `check` fails
when it differs from what `grants` would write. `dotnet run --file scripts/repo-conventions.cs --
grants` now writes three assemblies rather than two. `BNAQ1005` reads the grants compiled into the
assembly, so a missed regeneration fails there too.

📏 **4. `repo-conventions check` classifies the new project as role "other" and demanded net10.0 and
an `AutoVersioning` reference.** Both are now exemptions in `.github/repository.json` under
`props`:

| Rule | Reason recorded |
|---|---|
| `AutoVersioning` | The same as the other two projects: this is AutoVersioning itself, and a package cannot generate its own build |
| `TargetFramework` | **netstandard2.0 rather than the family's net10.0 — the maintainer's decision of 2026-11-19.** This package exists so a *consumer* can call `TryGetFromFile` at all, so reach is the point: netstandard2.0 covers .NET Framework 4.6.1+ and every modern .NET, while net10.0 would leave a consumer whose test project is on net8.0 still unable to call it. The sources are netstandard2.0-clean regardless, the analyzer compiling the same files |

⚠ **That TFM is effectively permanent once the id is published**, which is why it was put to the
maintainer rather than taken as an implementation detail. Multi-targeting `netstandard2.0;net10.0`
was offered and declined: it would put two `lib/` folders in a hand-written nuspec and oblige step
4's packaging test to assert both.

### Step 1, as built — the trim and the namespace

One commit, before anything moves, so that step 2's move stays a pure move.

**The namespace is `Bennewitz.Ninja.AutoVersioning`** on the files in `Versioning/`, while the rest
of the project stays in `…AutoVersioning.SourceGenerators`. The **assembly name does not change.**

⭐ **It cost no `using` directives at all, and that is a property of the name chosen rather than
luck.** `Bennewitz.Ninja.AutoVersioning` *encloses* both `…AutoVersioning.SourceGenerators` and
`…AutoVersioning.Tests`, so the compiler's walk up the enclosing namespaces resolves `BuildVersion`
from either without one. Both projects built with 0 warnings and 0 errors unchanged. A new top-level
namespace would have required a `using` in every call site.

**The trim**, decision 8:

| Member | Was | Now | Why |
|---|---|---|---|
| `DateTimeOffsetExtensions`, `GetQuarter` | `public` | `internal` | A public extension method on `DateTimeOffset` would land on **every `DateTimeOffset` in every consuming project**. One caller, `BuildVersion.cs` |
| `BuildVersion.Generate(UInt16, UInt16)` | `public` | `internal` | No caller anywhere. The relative-version algorithm stays public through `TryGetFromFile(FileInfo, int?, out BuildVersion)` |
| `FuzzyBuildVersionComparer` | `public` class | **deleted** | No caller; one mention in a comment |

⭐ **Deleting the comparer rather than making it internal was settled by evidence, which is why the
plan left the choice open.** It was the **only** thing in `Versioning/` that depended on the rest of
the generator — `FuzzyBuildVersionComparer.GetHashCode` called `Hashing/HashCodeUtility`. Kept, step
2 would have had to drag `Hashing/` into `.Abstractions` or link it there, which is outside the
plan's scope list. Deleted, `Versioning/` is self-contained and the move is exactly what the plan
describes.

⛔ **What that leaves behind, recorded rather than acted on: `Hashing/` now has no caller that
compiles.** `Hashing/AGENTS.md` named `FuzzyBuildVersionComparer.GetHashCode` as "the caller that
compiles on netstandard2.0"; the only others are `#else` branches in `Enums/` that do not compile on
this project's single target framework. Removing the directory is **not** in `plans/00002`'s scope,
and `xxHash32` is vendored with its verification notes, so that is a decision of its own rather than
a tidy-up inside this plan. Both `AGENTS.md` files now say so.

**Checked and not a problem:** `BNAQ1002` and `BNAQ1004` do not constrain the root namespace —
`BNAQ1002` is about serializer-DOM types reaching the public surface, `BNAQ1004` about a namespace
segment shadowing a referenced root such as `System`. Putting `BuildVersion` outside the assembly's
own name prefix breaks neither. This was checked **before** the edit, since a failure there would
have meant the namespace decision was unimplementable.

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

✅ **Closed in code 2026-11-19, and closed for consumers at `plans/00002` step 7's release.**
`Bennewitz.Ninja.AutoVersioning.Abstractions` ships `lib/netstandard2.0`, and
`PackagedConsumerTests` proves a consumer restoring the `.nupkg` can call `TryGetFromFile`. The
section below is the finding as it stood, kept for the reasoning that chose the route; the live
state is `## plans/00002` above.

The intent is for consumers to call it from their own tests to read the stamp back off a built
binary. They cannot. `Package.nuspec` ships the assembly to `analyzers\dotnet\cs` only, with
`developmentDependency: true` and `IncludeBuildOutput=false`, so the compiler loads it and it never
enters the consumer's reference set. There is no `lib/`. Confirmed by search: nothing outside this
repo calls `TryGetFromFile` or references `Bennewitz.Ninja.AutoVersioning.SourceGenerators`.

**Decided 2026-11-16, by the maintainer: a separate `.Abstractions` package**, the first row below.
`plans/00001` scopes it out deliberately, fixing only what `TryGetFromFile` computes, not what
reaches a consumer.

📋 **[`plans/00002`](plans/00002-tryget-from-file-reaches-a-consumer.md) is the plan**, written
2026-11-16, **approved 2026-11-17 and frozen**. Its four draft questions were all settled with
evidence before approval — the public surface trimmed (decision 8), AssemblyQuality pinned to the
generator (decision 9), `Pack.ps1` naming both ids, and a README of its own (decision 12). Steps 0
to 5 are done; what remains is the preflight dispatch and the release.

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

- **The diagnostic ids are `BNAV00`–`05`, and the prefix does not change again.** The family scheme
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
