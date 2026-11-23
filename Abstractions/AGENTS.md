# AGENTS.md — `Abstractions/`

`Bennewitz.Ninja.AutoVersioning.Abstractions`: the CalVer types a **consumer** can call. The
AutoVersioning package ships one analyzer assembly to `analyzers\dotnet\cs` with no `lib\`, so a
consumer's compiler loads it and the consumer's compilation never sees it — `BuildVersion.TryGetFromFile`
is correct, tested, and unreachable. This project is the `lib\` that fixes that.
[`plans/00002`](../plans/00002-tryget-from-file-reaches-a-consumer.md), approved and frozen; progress
and drift are in [`PROGRESS.md`](../PROGRESS.md).

| Path | What it holds |
|---|---|
| `Bennewitz.Ninja.AutoVersioning.Abstractions.csproj` | netstandard2.0, `AssemblyName` matching the package id, `RootNamespace` `Bennewitz.Ninja.AutoVersioning` |
| `Versioning/` | `BuildVersion` and the CalVer algorithm, and `FuzzyBuildVersionComparer`, which is **public API**. With its own `AGENTS.md`. Moved here from the repository root by step 2 |
| `Hashing/` | `HashCodeUtility` and a vendored `xxHash32`, both **`internal`**, with their own `AGENTS.md`. Moved here on 2026-11-19 when the comparer was restored: the comparer is published, the machinery behind its `GetHashCode` is not. ⚠ BCL types only, so this package still packs with an **empty dependency group** |
| `README.md` | The package's own nuget.org page, `plans/00002` decision 12. ⛔ **Absolute links only** |

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| ⛔ **The generator compiles these sources into itself and never references this project.** The root csproj excludes `Abstractions\**` from its glob and links `Abstractions\Versioning\**\*.cs` back in | The analyzer must be one self-contained assembly with no dependencies — code on the generator's path may use only what the compiler host supplies. A `ProjectReference` would also be `CS0436`, the same types arriving from source and from a reference | The `<Compile Include>` and the comment above it in `Bennewitz.Ninja.AutoVersioning.csproj` |
| ⛔ **The test project never references this project.** It references the generator, which carries its own copy of these types | Every `BuildVersion` call site becomes `CS0433`, the type existing in two referenced assemblies. `plans/00002` decision 10 | Nothing automated; the test project's single `ProjectReference` |
| `Abstractions\**` stays in the root csproj's `DefaultItemExcludes` | A nested project's `obj\` is **not** excluded automatically, so the root glob would compile this project's generated assembly-attribute file too, failing with `CS0579` | `DefaultItemExcludes` in the root csproj |
| The namespace here is `Bennewitz.Ninja.AutoVersioning`, never `…SourceGenerators` | A consumer would write `using …SourceGenerators;` to read a version number, from a package called `.Abstractions`. **Permanent after publication.** `plans/00002` decision 11 | `Versioning/AGENTS.md`; nothing automated |
| Anything `public` here is a supported API the moment this ships | `plans/00002` decision 8 trimmed three members for exactly this reason, before the id existed | `Versioning/AGENTS.md`'s trim table |

## Packaging

Packs **the SDK's way, with no `NuspecFile`** — unlike the generator, whose layout is unusual (one
DLL into `analyzers\dotnet\cs`, no `lib\`) and which therefore needs a hand-written file list. This
is an ordinary library, so the default `lib\netstandard2.0` placement is already right.

| Rule | Why | Guarded by |
|---|---|---|
| ⛔ **`IsPackable`, the package metadata and the id in `packages.push` change together** | `scripts/assert-packages.cs` fails the build on a packed id declared in neither list, and a declared id that does not pack fails the release on a missing file | `assert-packages` in CI's `build` job; `PackagingTests` |
| ⛔ **Every packable project is NAMED in `ci.yml`, `release.yml` and `Pack.ps1`** | **There is no solution file.** A bare `dotnet pack` binds to the root project alone, so a project not named simply never packs — and in `release.yml` that means pushing an id that was never built, *after* the first id has published permanently | `PackagingTests`; the comments at each pack step |
| ⛔ **It is compiled only via the root csproj's `ProjectReference` with `ReferenceOutputAssembly="false"`** | CI builds the root and test projects by path. Remove that and nothing compiles this project, which would break silently while every check stayed green | The `ProjectReference` and its comment in the root csproj |
| It carries its own `README.md` | `plans/00002` decision 12. ⚠ A package carrying **no** README passes `assert-packages` silently, its link check being guarded by `r.Readme is not null` | `PackagingTests.The_abstractions_package_carries_a_lib_and_a_readme` |
| `developmentDependency` is **not** set | Unlike the analyzer, this is a library a consumer compiles against, so the reference must flow | Nothing automated |

⭐ **A `lib`-less package cannot be produced here at all**, which is stronger than the test that
checks for one: with SDK packing, emptying the package raises `NU5128` (*"Add lib or ref assemblies
for the netstandard2.0 target framework"*), and `TreatWarningsAsErrors` in the root
`Directory.Build.props` turns that into a build error. The test remains because it reads the
`.nupkg` rather than the project, and because the README half of it **is** reachable: a
README-less package packs perfectly well.
