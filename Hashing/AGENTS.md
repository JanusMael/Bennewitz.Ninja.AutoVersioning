# AGENTS.md — `Hashing/`

Hash-code combining for the analyzer assembly. `FuzzyBuildVersionComparer.GetHashCode` is the
caller that compiles on netstandard2.0; the `#else` branches in `Enums/` that also call it do not.

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
