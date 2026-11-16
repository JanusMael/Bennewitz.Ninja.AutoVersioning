# 00002 — `TryGetFromFile` reaches a consumer, through a `.Abstractions` package

> Status: **draft, awaiting approval**. Supersedes nothing.

`BuildVersion.TryGetFromFile` reads this package's own CalVer stamp back off a built binary. It is correct, it is
tested, and **no consumer can call it.** `Package.nuspec` ships the one assembly to `analyzers\dotnet\cs` with
`developmentDependency: true` and `IncludeBuildOutput=false`, so the consumer's *compiler* loads it and the
consumer's *compilation* never sees it. There is no `lib/`. Confirmed by search: nothing outside this repository
calls `TryGetFromFile` or references `Bennewitz.Ninja.AutoVersioning.SourceGenerators`.

The intended use is a consumer's own tests — assert that a built artifact carries the stamp this build gave it,
which is exactly what `plans/00001` step 5 now does *inside this repository* and nobody else can do outside it.

The maintainer chose a separate `.Abstractions` package on 2026-11-16. This plan is how that is built.

## What exists today

| Where | What it does |
|---|---|
| `Bennewitz.Ninja.AutoVersioning.csproj` | netstandard2.0, `IsRoslynComponent`, assembly name `Bennewitz.Ninja.AutoVersioning.SourceGenerators`, `IncludeBuildOutput=false`, `SuppressDependenciesWhenPacking` |
| `Package.nuspec` | Lists the package's files one by one. One assembly, into `analyzers\dotnet\cs`, plus `build\` and `buildMultiTargeting\` copies of `Build.props` / `Build.targets`, the templates and `docs/MultiProjectBuildTimestamps.md` |
| `Versioning/` | `BuildVersion`, `DateTimeOffsetExtensions`, `FuzzyBuildVersionComparer`. **The whole of what a consumer would want** |
| `Hashing/` | `HashCodeUtility` and a vendored `xxHash32`. `BuildVersion` does not use them; `Enums/` and `Syntax/` do |
| `packages.push`, `packages.local` | The ids `release.yml` publishes, by name. `scripts/assert-packages.cs` fails CI on a packed id in neither |
| The nuget.org policy | **One** trusted-publishing policy for this repository, with a plural Glob Patterns field |

## The correction this plan is built on

⛔ **A second package id does NOT get its own trusted-publishing policy.** nuget.org mints **one API key per token
exchange, scoped to the one matching policy**. Two policies matching this owner, repository and workflow would
publish the first id permanently and reject the rest with `403` in the same `dotnet nuget push` — and a published
version can never be replaced. The existing policy's **Glob Patterns** field is plural for exactly this: widen it to
cover both ids. `PROGRESS.md` records this correction against an earlier, wrong note.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **The id is `Bennewitz.Ninja.AutoVersioning.Abstractions`** | The family names by suffix, and the glob that covers `Bennewitz.Ninja.AutoVersioning*` covers it without a second entry — to be confirmed against the policy's actual pattern before the first release, not assumed |
| 2 | **It carries `Versioning/` only**: `BuildVersion`, `DateTimeOffsetExtensions`, `FuzzyBuildVersionComparer` | They are what a consumer reads a stamp with. `Enums/`, `Syntax/` and `Hashing/` are generator machinery and reference Roslyn or exist only to serve it |
| 3 | **`Versioning/` moves into the new project; the generator project links the same files** | One copy of the algorithm. Two copies is the defect `plans/00001` step 5 exists to prevent, and it would be worse here — same language, same repository, no test between them |
| 4 | **The analyzer keeps shipping its own compiled copy and takes no package dependency** | The invariant that the package carries one assembly and no dependencies is what keeps the generator loadable in a consumer's compiler. A `PackageReference` from the analyzer would put a second assembly on the compiler's load path |
| 5 | **netstandard2.0**, matching the analyzer | A consumer's test project is whatever it is; netstandard2.0 reaches all of them, and this code has no API that needs more |
| 6 | **A row in `packages.push`, and the one policy's glob widened before the first release** | `scripts/assert-packages.cs` fails the run on a packed id that neither list declares, and `scripts/release-target.cs` resolves the ids to push from `packages.push` |
| 7 | **Both ids release together, from the one tag** | One version per release day is already the rule; two ids moving independently would need two version lines and a second answer to "which version is this repository on" |
| 8 | **The public surface is trimmed BEFORE the first release, in its own commit.** `GetQuarter` becomes `internal`; `BuildVersion.Generate(UInt16, UInt16)` and `FuzzyBuildVersionComparer` become `internal` or are deleted | Measured 2026-11-16: all three are used **nowhere**. `Generate(UInt16, UInt16)` has no caller in the repository at all; `FuzzyBuildVersionComparer` is named only in one comment; `GetQuarter` is called once, from `BuildVersion.cs` itself. Shipping them makes each a supported API permanently, and `GetQuarter` is the worst of the three — a public extension method on `DateTimeOffset` appears on **every `DateTimeOffset` in every consuming project**, which is a cost paid by people who never asked for it. The nested `Constants`, `Formats` and `VersionBuildInfo` are already `private`, so nothing else needs touching. The maintainer's call of 2026-11-16 |
| 9 | **`Architecture/AssemblyQualityTests.cs` pins its target assembly by a type that stays in the generator** | ⛔ It currently reads `private static readonly Assembly Shipped = typeof(BuildVersion).Assembly;`. `BuildVersion` moving to `.Abstractions` **silently redirects that whole test class to the new assembly**, and the generator — the thing a consumer's compiler loads, which is why those rules exist — stops being tested. **No test fails when this happens.** Either pin to a type that stays (`AssemblyInfoGenerator`), or run the rules over both assemblies, which is the better answer if the new one is to be held to the same bar |

### Settled 2026-11-16, by evidence rather than by guess

- **The public surface** — measured, three members are unused; decision 8 above.
- **AssemblyQuality's reach** — measured, `typeof(BuildVersion).Assembly` is a silent trap; decision 9 above.
- **`Pack.ps1` packs one project only.** It takes `$CsprojPath = Bennewitz.Ninja.AutoVersioning.csproj` and packs
  Debug and Release from it. It needs the second project added explicitly; there is no glob to inherit.

### Still to settle before approval

- **Does the new package get its own README, or a section in the existing one?** `scripts/assert-packages.cs`
  requires the packed README of **every** id in `packages.push` to link only absolutely, so whichever is chosen is
  a file that must satisfy that check. A shared README describes an analyzer to someone who installed a library;
  a second one is another document to keep in step with the first.

## Scope

**In:** a new project and its `.csproj`; `Versioning/` moved and linked back; `Package.nuspec` or a second nuspec;
`packages.push`; the policy's Glob Patterns; `Pack.ps1`; `PackagingTests`; the new assembly's `AGENTS.md`;
`Versioning/AGENTS.md`'s closing paragraph, which currently says these types reach no consumer; `README.md`;
`PROGRESS.md`.

**Out:**

- Any change to what the generator computes or stamps. `plans/00001` did that.
- A `lib/` added to the existing package — the dismissed option, which would publish the whole generator assembly as
  a runtime reference and expose Roslyn-dependent types that fail at runtime, `SuppressDependenciesWhenPacking`
  having stripped the dependency.
- The consumer's "not configured" reports, which are blocked on logs and unrelated.

⛔ **The release is made on the maintainer's word, as every release is**, and a first release of a new id is
irreversible in a way the others are not: the id is claimed on nuget.org permanently.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | **Decision 8's trim, in its own commit, before anything moves**: `GetQuarter` internal, the unused `Generate` overload and `FuzzyBuildVersionComparer` internal or deleted | The suite green with warnings as errors. Doing this first means the diff that moves `Versioning/` is a pure move, and the trim is reviewable on its own rather than hidden inside it |
| 2 | The new project, `Versioning/` moved into it, the generator project linking the same files; **decision 9's fix to `AssemblyQualityTests`** | `dotnet build -c Release` with warnings as errors, 0 warnings; the existing suite green and unchanged; the generator assembly still carries its own copy, confirmed in the packed output. ⛔ `AssemblyQualityTests` must be shown to still test the **generator** — assert the assembly's name rather than trusting `typeof(...).Assembly`, since the failure mode here is a test that silently changes target and keeps passing |
| 3 | Packaging: the nuspec, `packages.push`, `Pack.ps1` | `dotnet pack` into a **fresh** directory, then `assert-packages` — two ids, both declared, both READMEs linking absolutely. ⚠ Pack into a fresh directory: `assert-packages` reads every `.nupkg` it finds, and `packages/` is gitignored, so a stale one from an old release fails it misleadingly |
| 4 | A test that a consumer can actually call it: the `plans/00001` fixture consumer gains a reference to the packed `.Abstractions` and reads its own built assembly's stamp back | Green — and this is the whole point of the plan, so it is the step that proves it rather than the packaging |
| 5 | The documents: `README.md`, the new `AGENTS.md`, `Versioning/AGENTS.md`'s closing paragraph, `PROGRESS.md` | `scripts/repo-conventions.cs check` conforms |
| 6 | **The policy's Glob Patterns widened, and a blank-`version` dispatch of `release.yml` run as a credential preflight** | The preflight lists **both** ids and publishes nothing. ⛔ This must pass before any tag: the 403 described above is only visible here, and a tag that hits it has already published one id permanently |
| 7 | A release, on the maintainer's word | nuget.org lists both ids at the version |
