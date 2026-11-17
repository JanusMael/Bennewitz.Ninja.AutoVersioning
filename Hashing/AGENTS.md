# AGENTS.md — `Hashing/`

Hash-code combining for the analyzer assembly.

⛔ **Nothing that compiles on netstandard2.0 calls this directory any more.**
`FuzzyBuildVersionComparer.GetHashCode` was that caller and was deleted by `plans/00002` step 1, as
the only thing in `Versioning/` depending on the rest of the generator. The remaining call sites are
the `#else` branches in `Enums/`, which **do not compile on this project's only target framework**.

So this directory is, today, reachable code with no reachable caller. That is recorded rather than
acted on: removing it is not in `plans/00002`'s scope, and `xxHash32` is vendored with its
verification notes, so a deletion is a decision of its own. `PROGRESS.md` holds the finding.

| File | What it holds |
|---|---|
| `HashCodeUtility.cs` | `GetCompositeHashCode` and `CombineHashCodes`: each hash code laid out as little-endian bytes through `IntToByteUnion`, then hashed with `xxHash32` |
| `xxHash32.cs` | `xxHash32.ComputeHash`: XXH32 by Yann Collet, vendored and `internal` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **`xxHash32` stays vendored; do not replace it with `System.IO.Hashing`** | On netstandard2.0 that package pulls in `System.Buffers` and `System.Memory`, and the package ships one assembly with no dependencies | `Package.nuspec`; nothing automated |
| No `Span<T>`, `BinaryPrimitives` or `MemoryMarshal` here | The same: they come from `System.Memory` on netstandard2.0 | Nothing automated |
| A change to `xxHash32` is checked against the published vectors in its remarks, `XXH32("")` and `XXH32("abc")` | It is a fixed algorithm; the remarks record how the copy was verified | Nothing automated; no test covers this directory |
| `HashCodeUtility` returns hash codes for in-memory use only | `IntToByteUnion` assumes a little-endian machine, so values are not portable | Nothing automated |
