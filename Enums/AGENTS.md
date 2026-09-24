# AGENTS.md — `Enums/`

Reflection helpers over runtime enum `Type`s. Compiled into the analyzer assembly. Their one caller
is `AssemblyInfoSyntaxFactory.Identity.Flags`, through `FlagsEnumExtensions.SplitFlags`, which
`AssemblyInfoTemplate` does not currently use.

| File | What it holds |
|---|---|
| `EnumTypeInfo.cs` | An enum type's underlying type, values, default and `[Flags]`-ness |
| `EnumValue.cs` | One enum member, with its attributes |
| `EnumTypeInfoCache.cs` | A `ConcurrentDictionary` of `EnumTypeInfo` by `Type`, with an optional validity check |
| `FlagsEnumExtensions.cs` | `SplitFlags`, `CombineFlags`, `HasFlagsAttribute` |
| `ValueTypeExtensions.cs` | `ChangeType` overloads, enum-aware and culture-invariant |
| `ArrayExtensions.cs` | `ToArray<T>` over an untyped `Array` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **Before this code is put on the generator's path, `EnumTypeInfo.GetHashCode` and `EnumValue.GetHashCode` stop using `System.HashCode`** | On netstandard2.0 the `#if NETSTANDARD` branch takes `HashCode` from `Microsoft.Bcl.HashCode`, whose assembly `Package.nuspec` does not ship | Nothing automated |
| These types reflect over `System.Type`, never over the compilation's symbols | The runtime types are the analyzer's own (such as `AssemblyNameFlags`); a consumer's enums exist only as `INamedTypeSymbol` | Nothing automated |
| Conversions pass `CultureInfo.InvariantCulture` | The analyzer runs under the build machine's culture, as `Versioning/AGENTS.md` explains | Nothing automated |

No test covers this directory.
