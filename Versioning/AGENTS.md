# AGENTS.md — `Versioning/`

The CalVer algorithm. Compiled into the analyzer assembly; `AssemblyInfoGenerator` calls
`BuildVersion.Generate` and embeds `BuildVersion.ToString()` in `DirectoryBuildInfo.BuildRelease`.

| File | What it holds |
|---|---|
| `BuildVersion.cs` | `BuildVersion`: `Generate` (absolute `YEAR.QUARTER.MMdd.HHmm`, or relative to a first year), `TryGetFromFile` to read a stamp back off a built binary, and the culture-invariant `BuildTime` |
| `DateTimeOffsetExtensions.cs` | `GetQuarter`: `(Month + 2) / 3` |
| `FuzzyBuildVersionComparer.cs` | Equality within a revision tolerance; `Default` is 30 minutes |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **A change to `CalculateAbsoluteVersion`, `GetQuarter` or `GetBuildInfo` is made in `Build.props` too** | `Build.props` derives `AutoVersion` / `AutoPackageVersion` from the same `BuildTimestamp` in MSBuild; if the two drift, a package's version disagrees with its own assembly | Nothing automated |
| Nothing here consults the ambient culture: every format and parse passes `CultureInfo.InvariantCulture` | A non-Gregorian culture changes the month and day, so the `AssemblyVersion` itself; non-Latin digits make `Convert.ToUInt16` throw | `BuildVersionTests`, run under several cultures; `BuildVersionFromFileTests.TryGetFromFile_IsUnaffectedByAmbientCulture` |
| A stamp is decomposed arithmetically (`TryGetBuildDateTime`), never through a string | `MMdd` and `HHmm` are stored as integers and lose their leading zero; 08:45 comes back as `845` | `BuildVersionFromFileTests.TryGetFromFile_RecoversTheStampedDateAndTime` |
| `TryGetFromFile` returns false for a version it does not recognise; it validates before constructing | A `Try` method that throws `ArgumentOutOfRangeException` on month 13 breaks its callers | `TryGetFromFile_UnrecognisedVersion_ReturnsFalseWithoutThrowing` |
| `ToString()` stays printable ASCII with no quote character | It is interpolated raw into a string literal in the consumer's generated source | `BuildVersionTests.ToString_IsPrintableAsciiAndSafeToEmbed` |
| `FuzzyBuildVersionComparer.GetHashCode` leaves `Revision` out | Two versions it calls equal must hash alike; the relation is not transitive, as its remarks say | Nothing automated |

These types are public but reach no consumer: the package has no `lib/`, so `TryGetFromFile` is
callable only from this repository's tests. `PROGRESS.md` records the options for closing that.
