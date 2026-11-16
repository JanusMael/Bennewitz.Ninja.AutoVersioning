# 00001 — A Debug build holds its stamp to the day

> Status: **approved 2026-11-16**. Supersedes nothing.

Every build of a project that references this package recompiles it, however little changed, because every build a
second after the last takes a new stamp. `Build.props` takes `BuildTimestamp` from the clock at each evaluation, to
the second, and makes it a `CompilerVisibleProperty`; the SDK writes it into the project's generated editorconfig, and
that file is an input of `CoreCompile`. The stamp is also in each project's reference assembly, whose identity is its
`AssemblyVersion` and which carries the public constant `DirectoryBuildInfo.BuildRelease`, so a project that
references another is out of date twice over: against its own stamp, and against its reference. Nothing here says so:
not `README.md`, `docs/MultiProjectBuildTimestamps.md`, `Build.props` or `AGENTS.md`. A consumer found it by taking
its builds apart, where it was the largest single cost of a build in a mutation-testing harness. That consumer now
holds the stamp for the builds its harnesses start, and its other builds still pay.

A version per build is what this package is for, and a build that leaves the machine is a Release build: `dotnet pack`
and `dotnet publish` build Release when nobody says otherwise, and the release workflows that publish this family's
packages build `-c Release`. This plan keeps a stamp per build there and holds a Debug build's stamp to its day, so a
Debug build that changed nothing compiles nothing, and one that changed a method body compiles that one project.

Two defects in the same machinery are fixed with it, because both are reached by the change and neither is
discoverable by a consumer today: a stamp that does not parse is replaced silently, and a stamp made in the midnight
hour cannot be read back.

## What exists today

| Where | What it does |
|---|---|
| `Build.props` | Sets `BuildTimestamp` to `DateTime.Now` as `yyyyMMddHHmmss` at each evaluation, unless something set it first; derives `AutoVersion` and `AutoPackageVersion` from it in the same evaluation, arithmetically; lists it among eight `CompilerVisibleProperty` items. Its only check on the value is `'$(BuildTimestamp.Length)' == '14'`, which **gates the derivation without reporting anything**: a stamp of another length leaves both properties empty, in silence |
| That length guard, measured | Fourteen characters is not fourteen *digits*, and the derivation reads only positions 0 to 11. Measured against `HEAD`'s `Build.props`: `20261345000000` derives `2026.4.1345.0`, `20260230000000` derives `2026.1.230.0`, `20260105250000` derives `2026.1.105.2500`, and `2026010500050X` derives `2026.1.105.5` — the last because position 13 is never read. The generator rejects all four and stamps the clock instead, so MSBuild and the compiler produce **different versions from one input**, which is what `AGENTS.md` names as the cost of these two derivations drifting. `TryGetFromFile` already rejects 30 February and hour 25 as stamps this package could not have produced |
| `AssemblyInfoGenerator.ParseBuildTimestamp` | `DateTimeOffset.TryParseExact` against `yyyyMMddHHmmss`, invariant. On failure the generator takes the clock instead and **reports nothing**, so a consumer whose stamp is misspelled gets a version it did not ask for, and the `AutoPackageVersion` MSBuild derived from the same value is wrong in the same build |
| `AssemblyInfoGenerator`, `BuildVersion` | Stamp `AssemblyVersion` and `AssemblyFileVersion` to the minute, `YEAR.QUARTER.MMdd.HHmm`, and `DirectoryBuildInfo.BuildRelease` to the second |
| `BuildVersion.Constants.MinimumValueOfHHmm`, `= 100` | Floors the `HHmm` revision at `0100`, with the comment "the minimum VALID value for the UInt16 of `HHmm` is 0100". **That is wrong**: `HHmm` for 00:00 to 00:59 is `0` to `59`, which are valid times. So `TryGetBuildDateTime` rejects every build made in the midnight hour, `TryGetFromFile` returns false for it, and the strict `BuildVersion(FileVersionInfo)` constructor throws `ArgumentException` for it |
| `BuildVersion.Constants.MinimumValueOfMMdd`, `= 101` | Floors the `MMdd` build number at `0101`, 1 January. This one is right, and it is what rejects the unstamped `1.0.0.0` |
| The SDK's `GenerateMSBuildEditorConfigFileCore`, in `Microsoft.Managed.Core.targets` | Writes each `CompilerVisibleProperty`, as it stands when the target runs, into `obj/…/<project>.GeneratedMSBuildEditorConfig.editorconfig`. With every value unchanged the file is left alone |
| The SDK's `CoreCompile`, in `Microsoft.CSharp.Core.targets` | Counts `@(EditorConfigFiles)`, that file among them, and the reference assemblies of the projects it references among its `Inputs` |
| The SDK's `Sdk.props` | Imports `Microsoft.Common.props`, which imports `Directory.Build.props` and then NuGet's package props, this package's `Build.props` among them, before `Microsoft.NET.Sdk.props`, where an empty `Configuration` becomes `Debug` |
| `docs/MultiProjectBuildTimestamps.md` | Says that, captured at evaluation, "all projects normally land on the same value"; says nothing of incremental builds |
| `Build.ps1.template`, `Build.sh.template` | Pass one `-p:BuildTimestamp` per run, for an exact stamp across a build's projects |

The SDK's files were read in 10.0.404.

## What it costs

Measured on 2026-11-13 on a scratch consumer of `2026.4.1031`, whose `Build.props`, `Build.targets` and generator are
`HEAD`'s apart from line endings: four `net10.0` projects, `Core` ← `Model` ← `App`, and `Leaf` referencing `Model`
and `Core`, built as a solution. Each count is MSBuild's own decision in a detailed log, `Building target "CoreCompile"
completely` against `Skipping target "CoreCompile" because all output files are up-to-date`, attributed to its
project, and every build's decisions added up to the projects it built. The table was measured with SDK 10.0.404 and
again with 11.0.100, which agree.

| A Debug build after | Stamp as today | Stamp held, `-p:BuildTimestamp` |
|---|---|---|
| nothing changed | 4 of 4 | 0 of 4 |
| a method body edited in `Leaf`, which nothing references | 4 of 4 | 1 of 4 |
| a method body edited in `Core`, which the other three reference | 4 of 4 | 1 of 4 |

- **Why each project recompiled**, as MSBuild logged it on an unchanged tree: `Core` because
  `Core.GeneratedMSBuildEditorConfig.editorconfig` was newer than its output, the one line of it that differed being
  `build_property.BuildTimestamp`; `Model`, `App` and `Leaf` because `Core`'s reference assembly,
  `Core\obj\Debug\net10.0\ref\Core.dll`, was newer than their outputs. That reference assembly changed with each
  stamp, and under a held stamp a body edit in `Core` left it byte for byte the same, which is why the other three
  skipped.
- **What it costs in time** is a consumer's own measurement, reported to this repository: on a quiet box, medians,
  9.8 s against 4.1 for a build after an edit to a test file, and 9.3 against 8.3 after an edit anywhere else. On a
  loaded box, single samples, four projects took 15.5 to 16.4 s unchanged against 4 to 4.7 with the stamp held. The
  scratch consumer's own times separate nothing, on a box other work held at 99 to 100% CPU: its four small projects
  spend a build mostly on work that is not compiling.
- **A stamp per evaluation is not one stamp per build.** Of 24 builds that took their stamps from the clock, on either
  SDK, 19 stamped their projects with more than one value, up to three: a project takes its stamp when the build first
  asks for it, and a `dotnet publish` stamped `Core` a second after the projects that reference it. None of the 24
  crossed a minute, so the version agreed throughout; `BuildRelease`, which carries the seconds, mostly did not.

## Which builds need a stamp of their own

- **Release** is the build that leaves the machine. With no `-c` given, `dotnet pack` and `dotnet publish` reached
  `Build.props` with `Configuration` already `Release`, and the release workflows that publish this family's packages
  build `-c Release`. They build a clean checkout, which compiles every project anyway.
- **Debug** is the inner loop: `dotnet build`, `dotnet test`, `dotnet run` and an IDE, building again and again over a
  tree that changed by a line or not at all. That is where every project recompiles on every build.

## The options

Each was measured in the consumer of *What it costs*, with SDK 10.0.404: *still unless something changed* in the one
form that needs no new code, the generator taking the clock when the compiler runs, which is what it does today with
a stamp that does not parse; the two holds as a consumer's `Directory.Build.props` would write them. That file is
imported where `Build.props` is, before the SDK's `Configuration` default, with nothing between the two touching
`Configuration` or `BuildTimestamp`, so it sees what `Build.props` would see.

| | Do nothing | Document the cost | Still unless something changed | **Still for Debug, to the day** | To the minute |
|---|---|---|---|---|---|
| An unchanged Debug build compiles | 4 of 4 | 4 of 4, or 0 where a consumer holds the stamp | 0 of 4 | **0 of 4** | 0 of 4 within the minute, 4 of 4 after it |
| After a body edit in `Core` | 4 of 4 | 4 of 4, or 1 where a consumer holds the stamp | 4 of 4: a new stamp is a new reference assembly | **1 of 4** | 1 of 4 within the minute, 4 of 4 after it |
| What a version means | The minute the project was evaluated | As today | The time the project last compiled | **Release as today; Debug its day, revision 0** | As today; `BuildRelease`'s seconds read 00 |
| `AutoVersion`, `AutoPackageVersion` | As today | As today | Empty: MSBuild cannot read a stamp the compiler takes | **Debug's `AutoVersion` ends `.0`; `AutoPackageVersion`, the date, is unchanged** | As today |
| `PublicVersion` | Untouched | Untouched | Untouched | **Untouched** | Untouched |
| A build's projects | Agree to the minute, bar a boundary | As today | Each carries its own compile time | **Debug: one stamp; Release as today** | `BuildRelease` agrees wherever the version does |

A Release build behaves as a Debug one under every option but the hold, which leaves it as today: an unchanged Release
build compiled 4 of 4. `PublicVersion` is the consumer's own string, mapped from a CI variable or a tag; when it
changes every project recompiles, rightly, under any of the five.

**The hold has a boundary of its own**, accepted: two Debug builds either side of midnight take different days, so the
first Debug build of a day recompiles everything once. That is one full build a day against one every build, and it is
why every test asserting a count passes its stamp explicitly instead of relying on the default.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **A Debug build takes its day as `BuildTimestamp`**: midnight, in the stamp's own format, when nothing set the stamp first and `Configuration` is `Debug` or empty as `Build.props` evaluates. Every other build keeps `DateTime.Now`, as today | Measured: an unchanged Debug build compiles 0 of 4, and 0 of 3 for `Leaf` built alone without `-c`; a body edit in `Leaf` compiles 1 of 4, and one in `Core` 1 of 4. The day is the resolution of `AutoPackageVersion`, so the hold leaves it as it is; a finer hold recompiles everything whenever it ticks over, as the minute did. Two Debug builds of one day carry one version, as they already carry one `AutoPackageVersion`; `BuildRelease` says they were built at 00:00:00 |
| 2 | **An empty `Configuration` counts as `Debug`** | `Build.props` evaluates before the SDK gives `Configuration` its default. A solution build, `-c`, `dotnet pack` and `dotnet publish` pass it as a global property, and `dotnet build` of one project does not. Measured: written as `== 'Debug'` alone, the hold held a solution build, 0 of 4, and not `dotnet build src/Leaf/Leaf.csproj`, 3 of 3, its props reading `Configuration` as empty. Re-measured 2026-11-16 at the evaluation point NuGet imports the package props at, which is where a `Directory.Build.props` is read: `dotnet build` with no `-c` gives `[]` and `dotnet run` gives `[]`, while `-c Debug` gives `Debug`, and `dotnet pack` and `dotnet publish` with no `-c` both give `Release`. ⚠ Reading `Configuration` from the project *body* instead reports `Debug` for every one of those, which is the SDK's default already applied — a probe placed there measures nothing about this decision. A project that sets `Configuration` in its own body, after `Build.props`, is read as Debug |
| 3 | **Every configuration but `Debug` keeps a stamp per build**, Release among them | Release is what leaves the machine, from a clean checkout that compiles everything anyway, so holding its stamp would cost the version its meaning and save that build nothing. A configuration of another name fails towards today's behaviour |
| 4 | **A stamp set before `Build.props` still wins**: `-p:BuildTimestamp`, an environment variable, an assignment in `Directory.Build.props` | The condition that protects one today stays, and one of the two lines always sets the stamp, so it is never left empty. A consumer that wants a stamp per Debug build writes today's default in its own `Directory.Build.props`. A project that reads its own version as it runs — a service logging `Assembly.GetName().Version` at start-up — can take one per build in its own project file, as a test fixture needing a fixed stamp already pins one; a project nothing references pays one compile for it |
| 5 | **A stamp that is set and does not parse is reported, not replaced in silence: one new diagnostic, `BNAV05`, an Error.** ⛔ **This fails a consumer build that succeeds today** — a typo'd stamp currently builds, on the clock — so it is a breaking change and the release notes say so, as the `BNAV` rename's did. Error rather than Warning is the maintainer's call of 2026-11-16, with the Warning and the warn-then-error options both put and declined | Today `ParseBuildTimestamp` fails and the generator takes the clock, reporting nothing, while MSBuild derives `AutoVersion` and `AutoPackageVersion` from the same bad value by string surgery. The consumer asked for a stamp and got a different one, and a package can carry a version that disagrees with the assembly inside it — the one thing `AGENTS.md` names as the cost of these two derivations drifting. An Error rather than a Warning because the fallback hides a wrong *shipped* version and the earlier failure is the cheaper one; the id is new, so no existing suppression changes meaning. It fires only when the stamp is non-empty and unparseable: absent is the normal case, and the held stamp of decision 1 always parses |
| 6 | **`Build.props` derives nothing from a stamp it cannot validate, and a target reports why.** The derivation's own condition becomes the full check — fourteen digits, month 1 to 12, day no more than `$([System.DateTime]::DaysInMonth(year, month))`, hour no more than 23, minute no more than 59 — so `AutoVersion` and `AutoPackageVersion` are **empty rather than wrong**; a target then errors when `BuildTimestamp` is non-empty and the derivation declined it | Not redundant with `BNAV05`, and the reason is the one thing `Build.props` is deliberately ungated for: a project that uses `AutoPackageVersion` **without** enabling the generator runs no generator, so no `BNAV05` can fire, and it is exactly that project which consumes the derived version — measured above, it would pack `2026.1.230`. **The check has to live in the condition, not only in the target**, because `dotnet msbuild -getProperty:AutoPackageVersion` is an evaluation: it never runs a target, so a target alone would leave the wrong value readable by exactly the tooling that reads versions that way. `DaysInMonth` is a static on `System.DateTime` and works in evaluation without throwing — measured, 28 for February 2026 and 29 for 2024 — so the full validation needs no target and cannot fail the build by accident. A length test is not the check: all four measured divergences are fourteen characters long |
| 7 | **`TryGetFromFile`'s midnight floor is a defect and is fixed**: `MinimumValueOfHHmm` becomes `0`, and its comment is corrected | `HHmm` for 00:00 to 00:59 is `0` to `59`. The floor of `100` makes `TryGetFromFile` return false for every build made in the midnight hour and the strict constructor throw for it, and under decision 1 a held Debug stamp is revision `0`, so every held stamp would read back as not stamped. The floor also buys almost nothing: `MinimumValueOfMMdd` is what rejects the unstamped `1.0.0.0`, and a four-part version that is not a stamp is accepted in the other 23 hours regardless. Two consequences, both accepted and both documented by decision 8. A three-part `YYYY.Q.MMdd` file version reads as midnight on that day, because `FileVersionInfo` reports an absent fourth part as `0` and this package always stamps four. And **reading a held Debug stamp back now answers a time rather than declining**: today `TryGetFromFile` returns false for it, which reads as "not a stamp I made"; afterwards it returns 00:00:00 that day, which is the stamp faithfully and *not* the instant the build happened. That is the honest cost of holding a stamp, and the decision is to say so in the documents rather than to let a defect stand in for it. Accepting both consequences, rather than reading the version string's part count or holding Debug to 00:01 to clear the floor, is the maintainer's call of 2026-11-16 |
| 8 | **The cost is written down**: the README's *Configuration Reference*, *Timestamp Consistency* and *Diagnostics*, `docs/MultiProjectBuildTimestamps.md`, the comment above `BuildTimestamp` in `Build.props` and an `AGENTS.md` invariant, with the design document's consistency claim corrected to what was measured | Nothing says it today. A consumer found it by taking its builds apart, and holds the stamp in its harnesses without anything here saying how |
| 9 | **`Build.ps1.template` and `Build.sh.template` keep passing a stamp, and say that it overrides the hold** | They exist for an exact stamp and git's metadata. A Debug build's stamp is exact without them now, so a consumer that runs them in the inner loop drops one line |
| 10 | **A test builds a consumer and counts `CoreCompile`** | Nothing automated guards what `Build.props` evaluates to. This behaviour lives in the order MSBuild evaluates files in, which only a build shows |
| 11 | **A test asserts that the two derivations agree**, over a table of stamps, and that the version MSBuild derives is the version the generator stamps | `AGENTS.md` carries *the `AutoVersion` derivation in `Build.props` computes what `CalculateAbsoluteVersion`, `GetQuarter` and `GetBuildInfo` compute* as an invariant whose guard is **"nothing automated"**, and no test reads `AutoVersion` or `AutoPackageVersion` at all. It is a second expression of one algorithm in a language that cannot share code with it, so only a comparison keeps it honest, and the leading zero is where it would go wrong silently: both reach `105` for 5 January, one by `Convert.ToUInt16("0105")` and the other by `1 * 100 + 5`. This plan changes one half of it, which is the moment to guard it. `BuildVersion.Generate(DateTimeOffset)` is public, so the comparison needs no new API |

Decisions 1 to 4 are these two lines in place of the one that sets `BuildTimestamp` today:

```xml
<BuildTimestamp Condition="'$(BuildTimestamp)' == '' and ('$(Configuration)' == '' or '$(Configuration)' == 'Debug')">$([System.DateTime]::Today.ToString('yyyyMMddHHmmss'))</BuildTimestamp>
<BuildTimestamp Condition="'$(BuildTimestamp)' == ''">$([System.DateTime]::Now.ToString('yyyyMMddHHmmss'))</BuildTimestamp>
```

### Dismissed

- **A stamp that stays still unless something changed.** In its measured form an unchanged build compiled 0 of 4 and
  an edit in `Leaf` 1 of 4, the stamp fresh on each recompile through the compiler server and without it; but an edit
  in `Core` compiled 4 of 4, because a project's new stamp is a new reference assembly, so an edit low in a graph still
  recompiles everything above it. MSBuild read `AutoVersion` and `AutoPackageVersion` as empty, so a package could no
  longer take its version from the stamp inside it, which is what `AutoPackageVersion` is for, and each project carries
  its own compile time, the drift `docs/MultiProjectBuildTimestamps.md` exists to prevent. Deciding in MSBuild instead,
  by keeping the last stamp in `obj` while nothing else changed, means replicating `CoreCompile`'s input list, which is
  the SDK's, in a target that runs after the evaluation `AutoPackageVersion` is derived in, around state that can be
  lost with nothing to report it.
- **A hold for local builds rather than Debug ones.** A held local build would mean what a held Debug build means;
  what differs is which builds are caught, and the SDK gives no signal for that, 10.0.404 assigning
  `ContinuousIntegrationBuild` nowhere, while the README says this package reads no CI flag. Telling CI providers
  apart by their variables fails unsafe: a CI the list does not know would release day-held versions.
- **The stamp to the minute**, the version's own resolution. The next minute recompiles everything, which is the inner
  loop's case, and `BuildRelease` loses its seconds.
- **Today's default, with a recipe for consumers.** Every consumer would carry the same two conditions, decision 2's
  trap included, and the `== 'Debug'` form shows how easily it is written wrong.
- **Doing nothing.** The cost stays, unexplained, for every consumer.
- **A coarser hold, a week or a constant.** The day is the coarsest that leaves `AutoPackageVersion` alone.
- **Carrying the stamp to the compiler another way, or making `DirectoryBuildInfo` internal.** Whatever carries the
  stamp into the assembly is an input of `CoreCompile`, and `AssemblyVersion` is the reference assembly's identity,
  so either way every stamp is a new reference assembly.
- **`BNAV05` as a Warning** (decision 5). It would be enough wherever warnings are errors and silent everywhere else,
  which is the wrong way round for a defect whose symptom is a wrong version on a published package.
- **Keeping the midnight floor and teaching the hold to avoid it** (decision 7), by holding Debug to 00:01 rather than
  00:00. It would leave the floor's real victim — a Release build made between 00:00 and 00:59 — unfixed, and encode
  a bug's workaround in the stamp every Debug build carries.
- **Keeping the length test as the whole of decision 6's check.** `'$(BuildTimestamp.Length)' == '14'` is what exists,
  and all four measured divergences pass it; one of them, `2026010500050X`, passes it with a non-digit in a position
  the derivation never reads.
- **Leaving decision 6 to `BNAV05` alone.** It would cover every project that enables the generator and no project
  that only uses `AutoPackageVersion`, which is the case `Build.props` is ungated to serve.
- **Reading the file version's part count to tell a three-part version from a midnight stamp** (decision 7). It means
  going back to the string this type decomposes arithmetically on purpose, to separate two cases that a consumer of
  this package cannot produce: it always stamps four parts.

## Scope

**In:** decisions 1 to 4 in `Build.props`; `BNAV05` in `AssemblyInfoGenerator`, with its row in
`AnalyzerReleases.Unshipped.md` and in the README's *Diagnostics* table; decision 6's target in `Build.props`;
decision 7 in `BuildVersion`, with `BuildVersionFromFileTests` extended; the test of decision 10 and its fixture;
decision 11's `VersionDerivationTests`, and the `AGENTS.md` invariant row it turns from "nothing automated" into a
named guard;
decisions 8 and 9 in `README.md`, `docs/MultiProjectBuildTimestamps.md`, `Build.props`'s comments, both build
templates and `AGENTS.md`; `PROGRESS.md`; a release carrying them, which rolls `BNAV05` into
`AnalyzerReleases.Shipped.md` under its version and whose hand-written notes state `BNAV05` as a breaking change,
with the two reading-back consequences of decision 7.

**Out:**

- Any consumer's change: its pin; a consumer's harnesses, which hold the stamp for the builds they start and may go
  on doing so; a consumer's start-up line that reads its own version (decision 4).
- **`TryGetFromFile` being unreachable by consumers at all**, because the package ships no `lib/`. That is the open
  item in `PROGRESS.md`, it wants a separate package id and a trusted-publishing policy of its own, and it is the
  maintainer's call. Decision 7 fixes what the method computes; what reaches a consumer is that item's business.
- What the stamp costs an IDE's design-time builds, which nothing here measured.

⛔ **The release is made on the maintainer's word, as every release is.**

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Decision 10: `IncrementalBuildTests`, over a fixture consumer of the measured shape under `Tests/fixtures`. Each test copies it outside the repository, so no `Directory.*` file here reaches it; imports this repository's `Build.props` from the fixture's `Directory.Build.props` and `Build.targets` from its `Directory.Build.targets`, where NuGet's imports fall relative to the SDK's `Configuration` default and the project body; loads the generator the test run built; builds with node reuse off, under a directory taken from the platform's own temp location rather than a hardcoded path; passes an explicit `-p:BuildTimestamp` wherever the assertion is not about the default, so no case can straddle midnight; and reads each project's `CoreCompile` decision from a detailed log. It also records what `Configuration` reaches the package props for `dotnet build`, `dotnet run`, `dotnet test`, `dotnet pack` and `dotnet publish` without `-c`, since decision 2 rests on it and `dotnet test` is the one inner-loop command still unmeasured | Red against today's `Build.props`: a second Debug build of an unchanged tree compiles 4 of 4, the failure naming `GeneratedMSBuildEditorConfig.editorconfig`. Each test fails too when the decisions it read do not add up to the projects it built, so a log it cannot read fails rather than passing as nothing compiled |
| 2 | Decision 7: `MinimumValueOfHHmm` to `0` and its comment corrected | `BuildVersionFromFileTests` gains, red first against today's constant: `2026.2.429.0` read back as 00:00 and `2026.2.429.59` as 00:59; `…60` and `…99` still rejected, minute out of range; a three-part `2026.4.1116` read as that day at 00:00, pinning decision 7's accepted consequence. The existing `1.0.0.0` row still passes, rejected by `MinimumValueOfMMdd`, and every other existing row is untouched |
| 3 | Decisions 1 to 4 in `Build.props` | `IncrementalBuildTests` green: an unchanged Debug build compiles 0 of 4 as a solution and 0 of 3 as one project with no `-c`; a body edit in the fixture's root library 1 of 4; an unchanged Release build 4 of 4; a stamp given with `-p:`, in the environment and in the fixture's `Directory.Build.props` is the one used, and `AutoVersion` ends `.0` in Debug while `AutoPackageVersion` is the date in both, each read with `dotnet msbuild -getProperty`. A held Debug stamp read back off the built assembly by `TryGetFromFile` is recognised, which is step 2's fix holding under step 3's change. Every existing test passes |
| 4 | Decisions 5 and 6: `BNAV05` with its release-tracking and README rows; the derivation's own validating condition; the reporting target | A fixture build reports `BNAV05` and the `Build.props` error, each asserted by id rather than by message, for every stamp measured above — `not-a-stamp`, `20261345000000`, `20260230000000`, `20260105250000` and `2026010500050X` — and neither for a well-formed one. The same fixture **with `GenerateAutoVersionedAssemblyInfo` false** reports the `Build.props` error and no `BNAV05`, which is decision 6's whole reason. `dotnet msbuild -getProperty:AutoPackageVersion`, which runs no target, answers **empty** rather than `2026.1.230` for 30 February — the assertion that the validation is in the condition and not only in the target. 29 February 2024 is accepted and 2026 rejected, pinning `DaysInMonth` against a hardcoded 31. `BNAV00` to `BNAV04` are unchanged. `RS2000`/`RS2001` pass, which they do only when every id has a release-tracking row |
| 5 | Decision 11: `VersionDerivationTests`. For each stamp in one table it reads `AutoVersion` and `AutoPackageVersion` with `dotnet msbuild -getProperty` over a project importing this repository's `Build.props` — an evaluation, so no compile — and compares them with `BuildVersion.Generate(DateTimeOffset)` for the same instant, parsed with `yyyyMMddHHmmss` and `AssumeLocal`, as the generator parses it. One case goes end to end over step 1's fixture: build it with a given stamp, then assert that MSBuild's `AutoVersion` equals the built assembly's own `AssemblyFileVersion`, read with `FileVersionInfo` | Green, and red if either half is changed alone. The table holds the leading-zero and boundary cases measured in this plan — `20260105000500` → `2026.1.105.5`, `20260105084500` → `2026.1.105.845`, `20260913161300` → `2026.3.913.1613`, `20260401000000` → `2026.2.401.0`, `20260331235900` → `2026.1.331.2359`, `20261231235900` → `2026.4.1231.2359`, `20261101000000` → `2026.4.1101.0` — and asserts `AutoPackageVersion` is the first three parts of `AutoVersion` in each. The end-to-end case is what would catch a drift that both reimplementations happened to share |
| 6 | Decisions 8 and 9: the documents | `scripts/repo-conventions.cs check` conforms; `scripts/assert-packages.cs` passes on the packed README; `README.md`, `Directory.Build.props.template` and `BNAV04` still describe one setup, there being nothing new for a consumer to set |
| 7 | The `AGENTS.md` invariants — *a Debug build holds its stamp to its day and every other build takes one*, guarded by `IncrementalBuildTests`, and *a stamp that is set and does not parse is reported, never replaced in silence*, guarded by `BNAV05` and decision 6's target — and **the existing row for the two derivations not drifting, whose guard stops being "nothing automated" and becomes `VersionDerivationTests`**; and `PROGRESS.md` | `check` conforms. The invariant table names a real guard for every row it can |
| 8 | A release, on the maintainer's word, by `AGENTS.md`'s checklist, moving `BNAV05` from `AnalyzerReleases.Unshipped.md` to `Shipped.md` under its version | nuget.org lists the version |
