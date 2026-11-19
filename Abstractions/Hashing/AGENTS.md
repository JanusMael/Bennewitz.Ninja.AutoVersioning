# AGENTS.md — `Abstractions/Hashing/`

Hash-code combining. ⛔ **Both types are `internal`, and both are compiled into two assemblies**:
`Bennewitz.Ninja.AutoVersioning.Abstractions`, which owns these files, and the generator, which
**links** them (see the `<Compile Include>` in the root csproj). Neither assembly references the
other.

⭐ **Its caller is `FuzzyBuildVersionComparer.GetHashCode`**, which *is* a supported public API of
the `.Abstractions` package — so the tooling is internal while the thing it serves is not. That is
the maintainer's decision of 2026-11-19. It also resolves a gap this directory briefly had:
`plans/00002` step 1 deleted the comparer, leaving nothing here with a caller that compiles, since
the `#else` branches in `Enums/` do not build on netstandard2.0. Restoring the comparer restored the
caller. `PROGRESS.md` has that history.

⚠ **Nothing here may become `public`.** It is in a package now, so a public member would be a
supported API, and these hash codes are explicitly not fit to be one — see the portability rule
below.

| File | What it holds |
|---|---|
| `HashCodeUtility.cs` | `GetCompositeHashCode` and `CombineHashCodes`: each hash code laid out as little-endian bytes through `IntToByteUnion`, then hashed with `xxHash32`. `internal` |
| `xxHash32.cs` | `xxHash32.ComputeHash`: XXH32 by Yann Collet, vendored and `internal` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **`xxHash32` stays vendored; do not replace it with `System.IO.Hashing`** | On netstandard2.0 that package pulls in `System.Buffers` and `System.Memory`. The analyzer must ship one assembly with no dependencies, and `.Abstractions` packs with an **empty dependency group** | `Package.nuspec`; `PackagingTests`; nothing automated for the dependency group |
| ⛔ **Nothing here becomes `public`** | These files are now inside a published package, where `public` means a supported API — and a non-portable, in-memory-only hash code is the last thing to promise consumers | Nothing automated |
| No `Span<T>`, `BinaryPrimitives` or `MemoryMarshal` here | The same: they come from `System.Memory` on netstandard2.0 | Nothing automated |
| A change to `xxHash32` is checked against the published vectors in its remarks, `XXH32("")` and `XXH32("abc")` | It is a fixed algorithm; the remarks record how the copy was verified | Nothing automated; no test covers this directory |
| `HashCodeUtility` returns hash codes for in-memory use only | `IntToByteUnion` assumes a little-endian machine, so values are not portable | Nothing automated |
