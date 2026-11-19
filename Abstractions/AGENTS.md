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
| `Versioning/` | `BuildVersion` and the CalVer algorithm, with its own `AGENTS.md`. Moved here from the repository root by step 2 |

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| ⛔ **The generator compiles these sources into itself and never references this project.** The root csproj excludes `Abstractions\**` from its glob and links `Abstractions\Versioning\**\*.cs` back in | The analyzer must be one self-contained assembly with no dependencies — code on the generator's path may use only what the compiler host supplies. A `ProjectReference` would also be `CS0436`, the same types arriving from source and from a reference | The `<Compile Include>` and the comment above it in `Bennewitz.Ninja.AutoVersioning.csproj` |
| ⛔ **The test project never references this project.** It references the generator, which carries its own copy of these types | Every `BuildVersion` call site becomes `CS0433`, the type existing in two referenced assemblies. `plans/00002` decision 10 | Nothing automated; the test project's single `ProjectReference` |
| `Abstractions\**` stays in the root csproj's `DefaultItemExcludes` | A nested project's `obj\` is **not** excluded automatically, so the root glob would compile this project's generated assembly-attribute file too, failing with `CS0579` | `DefaultItemExcludes` in the root csproj |
| The namespace here is `Bennewitz.Ninja.AutoVersioning`, never `…SourceGenerators` | A consumer would write `using …SourceGenerators;` to read a version number, from a package called `.Abstractions`. **Permanent after publication.** `plans/00002` decision 11 | `Versioning/AGENTS.md`; nothing automated |
| Anything `public` here is a supported API the moment this ships | `plans/00002` decision 8 trimmed three members for exactly this reason, before the id existed | `Versioning/AGENTS.md`'s trim table |

## State

**Not yet packable.** `IsPackable` is `false` until `plans/00002` step 3 adds the nuspec and the id
to `packages.push`: `scripts/assert-packages.cs` fails the build on a packed id declared in neither
list. ⛔ Flip `IsPackable`, add the nuspec and add the id **in one change**, never separately.

⚠ **There is no solution file in this repository**, and CI builds the root project and the test
project by path. This project is built only because the root csproj carries a `ProjectReference` to
it with `ReferenceOutputAssembly="false"` — a build dependency that passes no assembly. Remove that
and nothing in CI compiles this project at all. How it is to be **packed** is step 3's question, and
`dotnet pack` at the root packs the root project alone.
