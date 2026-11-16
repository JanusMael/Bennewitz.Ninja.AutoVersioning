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
| **A change to `CalculateAbsoluteVersion`, `GetQuarter` or `GetBuildInfo` is made in `Build.props` too** | `Build.props` derives `AutoVersion` / `AutoPackageVersion` from the same `BuildTimestamp` in MSBuild; if the two drift, a package's version disagrees with its own assembly | `Tests/…/Incremental/VersionDerivationTests.cs`. Each row is compared three ways — MSBuild against C#, and both against a literal approved in `plans/00001` — because two reimplementations can also drift *together*, which the first comparison cannot see |
| `MinimumValueOfHHmm` is `0`, not `100` | `HHmm` for 00:00 to 00:59 is `0` to `59`. A floor of `100` rejects every build made in the midnight hour, and a Debug build now holds its stamp to midnight, so every held stamp would read back as not stamped. `MinimumValueOfMMdd` is what rejects the unstamped `1.0.0.0` | `BuildVersionFromFileTests`, four accepting rows and two rejecting |
| Nothing here consults the ambient culture: every format and parse passes `CultureInfo.InvariantCulture` | A non-Gregorian culture changes the month and day, so the `AssemblyVersion` itself; non-Latin digits make `Convert.ToUInt16` throw | `BuildVersionTests`, run under several cultures; `BuildVersionFromFileTests.TryGetFromFile_IsUnaffectedByAmbientCulture` |
| A stamp is decomposed arithmetically (`TryGetBuildDateTime`), never through a string | `MMdd` and `HHmm` are stored as integers and lose their leading zero; 08:45 comes back as `845` | `BuildVersionFromFileTests.TryGetFromFile_RecoversTheStampedDateAndTime` |
| `TryGetFromFile` returns false for a version it does not recognise; it validates before constructing | A `Try` method that throws `ArgumentOutOfRangeException` on month 13 breaks its callers | `TryGetFromFile_UnrecognisedVersion_ReturnsFalseWithoutThrowing` |
| `ToString()` stays printable ASCII with no quote character | It is interpolated raw into a string literal in the consumer's generated source | `BuildVersionTests.ToString_IsPrintableAsciiAndSafeToEmbed` |
| `FuzzyBuildVersionComparer.GetHashCode` leaves `Revision` out | Two versions it calls equal must hash alike; the relation is not transitive, as its remarks say | Nothing automated |

These types are public but reach no consumer: the package has no `lib/`, so `TryGetFromFile` is
callable only from this repository's tests. `PROGRESS.md` records the options for closing that.

## What reading a stamp back now answers

`plans/00001` decision 7 lowered the `HHmm` floor to `0`. Two consequences follow, **both accepted
deliberately** rather than worked around, and neither is a defect to be "fixed" later:

- **A three-part `YYYY.Q.MMdd` file version reads as midnight on that day.** `FileVersionInfo`
  reports an absent fourth part as `0`, and this package always stamps four, so the two cases are
  indistinguishable — to a consumer of this package they cannot both arise. Separating them would
  mean going back to the version *string*, which `TryGetBuildDateTime` decomposes arithmetically on
  purpose.
- **A held Debug stamp reads back as `00:00:00` that day, rather than being declined.** Before the
  fix `TryGetFromFile` returned false for it, which read as "not a stamp I made". It now answers the
  stamp faithfully — and the stamp is *not* the instant the build happened. That is the honest cost
  of holding a Debug stamp to its day, and the documents say so rather than letting the floor stand
  in for it.
