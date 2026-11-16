using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Bennewitz.Ninja.AutoVersioning.SourceGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Incremental;

/// <summary>
/// The guard for the one invariant this repository had no automated check for: the CalVer
/// derivation exists twice — once in C# as <see cref="BuildVersion"/>, which stamps the assembly,
/// and once in MSBuild as <c>Build.props</c>' <c>AutoVersion</c> / <c>AutoPackageVersion</c>, which
/// is what a package takes its version from. One <c>BuildTimestamp</c> goes into both.
/// </summary>
/// <remarks>
/// <para>
/// If the two drift, a package carries a version that disagrees with the assembly inside it, and
/// nothing fails: both halves are internally consistent, each produces a plausible version, and the
/// disagreement is only visible when something compares them. That is what this class does.
/// </para>
/// <para>
/// Each row is asserted three ways rather than two, because two reimplementations can drift
/// <i>together</i>. The MSBuild value is compared with the C# value — that catches an ordinary
/// one-sided change — and both are compared with a literal written down when the plan was approved,
/// which catches a change made to both halves at once. <c>plans/00001</c> decision 11.
/// </para>
/// <para>
/// The table is read by evaluation, not by building: <c>dotnet msbuild -getProperty</c> runs no
/// target and compiles nothing, so seven stamps cost seven evaluations rather than seven builds.
/// The one case that must build is the end-to-end one at the bottom, which is the only assertion
/// here that sees the two derivations meet on a real artifact.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class VersionDerivationTests
{
    private const string TargetFramework = "net10.0";

    /// <summary>
    /// The stamp table <c>plans/00001</c> approved: the leading-zero cases that a string-sliced
    /// derivation gets wrong, and the quarter and year boundaries.
    /// </summary>
    /// <remarks>
    /// <c>20260105000500</c> is 00:05, which is <c>5</c> and not <c>0005</c>; <c>20260105084500</c>
    /// is 08:45, which is <c>845</c>. <c>20260401000000</c> and <c>20260331235900</c> sit either
    /// side of a quarter boundary one minute apart, and <c>20261231235900</c> either side of a year.
    /// </remarks>
    [TestMethod]
    [DataRow("20260105000500", "2026.1.105.5", "2026.1.105", DisplayName = "00:05 is 5, not 0005")]
    [DataRow("20260105084500", "2026.1.105.845", "2026.1.105", DisplayName = "08:45 is 845, not 0845")]
    [DataRow("20260913161300", "2026.3.913.1613", "2026.3.913", DisplayName = "an ordinary afternoon")]
    [DataRow("20260401000000", "2026.2.401.0", "2026.2.401", DisplayName = "the first minute of Q2")]
    [DataRow("20260331235900", "2026.1.331.2359", "2026.1.331", DisplayName = "the last minute of Q1")]
    [DataRow("20261231235900", "2026.4.1231.2359", "2026.4.1231", DisplayName = "the last minute of the year")]
    [DataRow("20261101000000", "2026.4.1101.0", "2026.4.1101", DisplayName = "the first minute of Q4")]
    public void MSBuildAndBuildVersion_DeriveTheSameVersion(
        string stamp,
        string expectedVersion,
        string expectedPackageVersion)
    {
        using var fixture = FixtureConsumer.Create();

        // Both properties come from one evaluation: starting dotnet is nearly all this costs.
        var evaluated = fixture.EvaluateMany(
            ["AutoVersion", "AutoPackageVersion"],
            "-p:BuildTimestamp=" + stamp);

        var fromMSBuild = evaluated["AutoVersion"];
        var packageVersion = evaluated["AutoPackageVersion"];
        var fromCSharp = Derive(stamp);

        Assert.AreEqual(
            fromCSharp,
            fromMSBuild,
            $"For BuildTimestamp '{stamp}', Build.props derived '{fromMSBuild}' and BuildVersion derived "
                + $"'{fromCSharp}'. One input must not produce two versions: whichever half changed, the "
                + "other has to change with it, or a package will carry a version its own assembly denies.");

        Assert.AreEqual(
            expectedVersion,
            fromMSBuild,
            $"'{stamp}' was approved in plans/00001 as '{expectedVersion}'. Both halves now agree on "
                + $"'{fromMSBuild}' instead, so they drifted together rather than apart — which is "
                + "exactly the change the comparison above cannot see.");

        // AutoPackageVersion is AutoVersion without its HHmm, and it is derived first: Build.props
        // appends the time to it rather than slicing it back off. A fourth segment here would not be
        // cosmetic — it is read as an HHmm, so 2026.4.1116.1 means that day at 00:01.
        Assert.AreEqual(
            expectedPackageVersion,
            packageVersion,
            $"AutoPackageVersion for '{stamp}' should be the date alone.");

        Assert.AreEqual(
            3,
            packageVersion.Split('.').Length,
            $"AutoPackageVersion is '{packageVersion}'. A fourth segment is read as HHmm wherever this "
                + "package's versions are read, so 2026.4.1116.1 would mean that day at 00:01.");
    }

    /// <summary>
    /// The two derivations meeting on a real artifact: MSBuild's <c>AutoVersion</c>, computed in the
    /// consuming project's evaluation, against the <c>AssemblyFileVersion</c> the generator wrote
    /// into the assembly that same build produced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the case that would catch a drift both reimplementations happened to share, because
    /// nothing here is compared with a value a test author typed: each side computed its own from
    /// the one stamp, and the file is read back with <see cref="FileVersionInfo"/> from the Win32
    /// version resource the compiler synthesised — the same thing a consumer reads.
    /// </para>
    /// <para>
    /// The four integer parts are compared rather than <see cref="FileVersionInfo.FileVersion"/>,
    /// whose string form is the resource's own and need not be four dotted numbers.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void TheBuiltAssembly_CarriesTheVersionMSBuildDerived()
    {
        const string Stamp = "20260913161300";

        using var fixture = FixtureConsumer.Create();

        var log = fixture.Build("-p:BuildTimestamp=" + Stamp);

        var fromMSBuild = FixtureConsumer.ReadReportedValue(log, "FIXTURE-AUTOVERSION");
        var assembly = BuiltAssembly(fixture, "Leaf", "Debug");
        var versionInfo = FileVersionInfo.GetVersionInfo(assembly);
        var fromAssembly = string.Join(
            '.',
            versionInfo.FileMajorPart,
            versionInfo.FileMinorPart,
            versionInfo.FileBuildPart,
            versionInfo.FilePrivatePart);

        Assert.AreEqual(
            fromMSBuild,
            fromAssembly,
            $"MSBuild derived AutoVersion '{fromMSBuild}' from BuildTimestamp '{Stamp}', and the generator "
                + $"stamped '{fromAssembly}' into '{assembly}' in the same build. A package built here would "
                + "take the first and ship the second.");

        Assert.AreEqual(
            Derive(Stamp),
            fromAssembly,
            "And both should be what BuildVersion.Generate computes for that instant.");
    }

    /// <summary>
    /// Derives the version in C# exactly as <c>AssemblyInfoGenerator</c> does: the same format, the
    /// same culture and the same <see cref="DateTimeStyles.AssumeLocal"/>.
    /// </summary>
    /// <remarks>
    /// Parsed any other way this would not be the comparison it claims to be. <c>AssumeLocal</c> is
    /// what makes a wall-clock stamp mean the wall clock, and a stamp is written by one machine's
    /// MSBuild and read by that same machine's compiler.
    /// </remarks>
    private static string Derive(string stamp)
    {
        Assert.IsTrue(
            DateTimeOffset.TryParseExact(
                stamp,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var timestamp),
            $"'{stamp}' is a row in this table, so it has to parse the way the generator parses it.");

        return BuildVersion.Generate(timestamp).Version.ToString();
    }

    /// <summary>
    /// The assembly a fixture build produced, looked for where the SDK puts it and then where it
    /// staged it, so a missing one names both rather than failing as a null reference.
    /// </summary>
    private static string BuiltAssembly(FixtureConsumer fixture, string project, string configuration)
    {
        var candidates = new[]
        {
            Path.Combine(fixture.Root, "src", project, "bin", configuration, TargetFramework, project + ".dll"),
            Path.Combine(fixture.Root, "src", project, "obj", configuration, TargetFramework, project + ".dll"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"The fixture build produced no '{project}.dll'. Looked in:{Environment.NewLine}"
                + string.Join(Environment.NewLine, candidates));
    }
}
