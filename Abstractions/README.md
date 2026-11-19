# Bennewitz.Ninja.AutoVersioning.Abstractions

Read a [Bennewitz.Ninja.AutoVersioning](https://www.nuget.org/packages/Bennewitz.Ninja.AutoVersioning)
CalVer stamp back off a built assembly.

`Bennewitz.Ninja.AutoVersioning` stamps your assemblies with a version of the form
`YEAR.QUARTER.MMdd.HHmm`. It ships as a Roslyn analyzer, so your *compiler* loads it and your *code*
never sees it. This package is the small library that lets your code — a test, a diagnostic, a
support endpoint — turn one of those versions back into the moment it was stamped.

It is useful on its own, but it exists for one job in particular: **asserting in your own tests that
a built artifact carries the stamp this build gave it.**

## Install

```
dotnet add package Bennewitz.Ninja.AutoVersioning.Abstractions
```

Targets `netstandard2.0`, so it works from .NET Framework 4.6.1 onward and on every modern .NET. It
has no dependencies, and it does not require
[Bennewitz.Ninja.AutoVersioning](https://www.nuget.org/packages/Bennewitz.Ninja.AutoVersioning) to be
installed — though reading a stamp back is of limited use if nothing is stamping it.

## Use

```csharp
using Bennewitz.Ninja.AutoVersioning;

var assembly = new FileInfo(typeof(Thing).Assembly.Location);

if (BuildVersion.TryGetFromFile(assembly, out var version))
{
    Console.WriteLine(version!.Version);       // 2026.4.1119.1613
    Console.WriteLine(version.BuildDateTime);  // 2026-11-19 16:13 +00:00
    Console.WriteLine(version.BuildTime);      // culture-invariant, safe to embed
}
```

`TryGetFromFile` returns `false` rather than throwing for a version it does not recognise — an
unstamped `1.0.0.0`, or a month of 13 — so it is safe to call on any assembly.

### In a test

```csharp
[TestMethod]
public void TheBuiltAssembly_CarriesThisBuildsStamp()
{
    var assembly = new FileInfo(typeof(Thing).Assembly.Location);

    Assert.IsTrue(BuildVersion.TryGetFromFile(assembly, out var version));
    Assert.AreEqual(DateTimeOffset.Now.Date, version!.BuildDateTime.Date);
}
```

## What the four parts mean

| Part | Meaning | Example |
|---|---|---|
| `YEAR` | The calendar year | `2026` |
| `QUARTER` | `(month + 2) / 3` | `4` |
| `MMdd` | Month and day, **as an integer** — leading zeros are gone, so 5 January is `105` | `1119` |
| `HHmm` | Hour and minute, likewise: 08:45 is `845`, and 00:05 is `5` | `1613` |

Because `MMdd` and `HHmm` are stored as integers, a stamp is decomposed arithmetically rather than
by parsing the version *string*.

## Two behaviours worth knowing

**A three-part version reads as midnight.** `FileVersionInfo` reports an absent fourth part as `0`,
and this package always stamps four, so `2026.4.1119` and `2026.4.1119.0000` are indistinguishable.

**A Debug build's stamp is midnight on purpose.** Since `2026.4.1116`, a Debug build holds its
`BuildTimestamp` to its day, so that repeated Debug builds stop recompiling every project. Such a
stamp reads back as `00:00:00` that day — faithfully the stamp, which is not the instant the build
ran. See
[`docs/MultiProjectBuildTimestamps.md`](https://github.com/JanusMael/Bennewitz.Ninja.AutoVersioning/blob/main/docs/MultiProjectBuildTimestamps.md),
which also measures what a clock-stamped build does across a large solution.

## Links

- [Source and issues](https://github.com/JanusMael/Bennewitz.Ninja.AutoVersioning)
- [The generator package](https://www.nuget.org/packages/Bennewitz.Ninja.AutoVersioning), which does
  the stamping

MIT licensed.
