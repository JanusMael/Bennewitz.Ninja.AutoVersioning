# AGENTS.md — `Abstractions/Versioning/`

The CalVer algorithm. ⛔ **Compiled into two assemblies**: it belongs to
`Bennewitz.Ninja.AutoVersioning.Abstractions`, which ships it as a `lib\` a consumer can call, and
the generator **links these same source files** so the analyzer stays one self-contained assembly
with no dependencies. `AssemblyInfoGenerator` calls `BuildVersion.Generate` and embeds
`BuildVersion.ToString()` in `DirectoryBuildInfo.BuildRelease`.

⚠ **A change here lands in both.** Neither assembly references the other; see `../AGENTS.md`.

⛔ **These files declare `namespace Bennewitz.Ninja.AutoVersioning`, not
`…AutoVersioning.SourceGenerators` like the rest of this project.** `plans/00002` decision 11, taken
before publication because a namespace cannot be changed after it: these types are to ship in
`Bennewitz.Ninja.AutoVersioning.Abstractions`, and a consumer should not write
`using …SourceGenerators;` to read a version number. The assembly *name* is unchanged.

⭐ **No `using` is needed anywhere for this**, which is why the rename cost nothing:
`Bennewitz.Ninja.AutoVersioning` encloses both `…SourceGenerators` and
`…AutoVersioning.Tests`, so the compiler's walk up the enclosing namespaces finds these types from
either. A new top-level namespace would not have that property.

| File | What it holds |
|---|---|
| `BuildVersion.cs` | `BuildVersion`: `Generate` (absolute `YEAR.QUARTER.MMdd.HHmm`, or relative to a first year), `TryGetFromFile` to read a stamp back off a built binary, and the culture-invariant `BuildTime` |
| `DateTimeOffsetExtensions.cs` | `GetQuarter`: `(Month + 2) / 3`. **`internal`, class and method** |
| `FuzzyBuildVersionComparer.cs` | Equality within a revision tolerance; `Default` is 30 minutes. **`public` — a supported API of the package.** Its `GetHashCode` is the only caller of `../Hashing/` that compiles |

⛔ **The public surface here is deliberately minimal, and was trimmed before the id was published**
(`plans/00002` decision 8, step 1). A public member becomes a supported API the moment this assembly
ships a `lib/`:

| Trimmed | Why |
|---|---|
| `DateTimeOffsetExtensions` and `GetQuarter` → `internal` | A **public extension method on `DateTimeOffset`** would appear on every `DateTimeOffset` in every consuming project. One caller, `BuildVersion.cs` |
| `BuildVersion.Generate(UInt16, UInt16)` → `internal` | No caller at all. The relative-version algorithm it reaches stays public through `TryGetFromFile(FileInfo, int?, out BuildVersion)` |

⚠ **`FuzzyBuildVersionComparer` is the exception, and it is `public` on purpose.** Step 1 deleted it
under that same decision; **the maintainer restored it as public on 2026-11-19**, which makes it a
supported API of the `.Abstractions` package. The frozen plan is not edited — `PROGRESS.md` records
the reversal. Its hashing tooling, `../Hashing/`, stays `internal`: the comparer is published, the
machinery behind it is not.

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **A change to `CalculateAbsoluteVersion`, `GetQuarter` or `GetBuildInfo` is made in `Build.props` too** | `Build.props` derives `AutoVersion` / `AutoPackageVersion` from the same `BuildTimestamp` in MSBuild; if the two drift, a package's version disagrees with its own assembly | `Tests/…/Incremental/VersionDerivationTests.cs`. Each row is compared three ways — MSBuild against C#, and both against a literal approved in `plans/00001` — because two reimplementations can also drift *together*, which the first comparison cannot see |
| `MinimumValueOfHHmm` is `0`, not `100` | `HHmm` for 00:00 to 00:59 is `0` to `59`. A floor of `100` rejects every build made in the midnight hour, and a Debug build now holds its stamp to midnight, so every held stamp would read back as not stamped. `MinimumValueOfMMdd` is what rejects the unstamped `1.0.0.0` | `BuildVersionFromFileTests`, four accepting rows and two rejecting |
| Nothing here consults the ambient culture: every format and parse passes `CultureInfo.InvariantCulture` | A non-Gregorian culture changes the month and day, so the `AssemblyVersion` itself; non-Latin digits make `Convert.ToUInt16` throw | `BuildVersionTests`, run under several cultures; `BuildVersionFromFileTests.TryGetFromFile_IsUnaffectedByAmbientCulture` |
| A stamp is decomposed arithmetically (`TryGetBuildDateTime`), never through a string | `MMdd` and `HHmm` are stored as integers and lose their leading zero; 08:45 comes back as `845` | `BuildVersionFromFileTests.TryGetFromFile_RecoversTheStampedDateAndTime` |
| `TryGetFromFile` returns false for a version it does not recognise; it validates before constructing | A `Try` method that throws `ArgumentOutOfRangeException` on month 13 breaks its callers | `TryGetFromFile_UnrecognisedVersion_ReturnsFalseWithoutThrowing` |
| `ToString()` stays printable ASCII with no quote character | It is interpolated raw into a string literal in the consumer's generated source | `BuildVersionTests.ToString_IsPrintableAsciiAndSafeToEmbed` |
| `FuzzyBuildVersionComparer.GetHashCode` leaves `Revision` out | Two versions it calls equal must hash alike; the relation is not transitive, as its remarks say | `FuzzyBuildVersionComparerTests.EqualInstancesHashAlike`, and `TheRelationIsDeliberatelyNotTransitive` for the warning itself |
| `FuzzyBuildVersionComparer` decodes `HHmm` before comparing revisions | 09:59 and 10:00 are one minute apart; the integers 959 and 1000 are 41 apart. Comparing them raw is wrong in both directions | `FuzzyBuildVersionComparerTests.TheRevisionIsDecodedFromHHmm_NotComparedAsAnInteger`, proven red by returning the revision undecoded |

**These types reach a consumer now.** `Bennewitz.Ninja.AutoVersioning.Abstractions` packs them into
`lib/netstandard2.0`, and `Tests/…/Packaging/PackagedConsumerTests.cs` proves it the only way that
counts: a one-project consumer restores the **`.nupkg`** from a local feed through a real
`PackageReference`, is stamped by the generator, and reads its own stamp back with `TryGetFromFile`.
That test is red when the package carries no `lib/`.

⚠ **The surface is still changeable today and permanent the moment it is published.** The id does
not exist on nuget.org until `plans/00002` step 7's release, which is the last point at which a
member can be renamed, removed or made `internal` without breaking somebody. Progress and drift are
in `PROGRESS.md`.

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
