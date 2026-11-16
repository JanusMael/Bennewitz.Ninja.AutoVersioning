using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Incremental;

/// <summary>
/// What happens to a <c>BuildTimestamp</c> that is set and is not a <c>yyyyMMddHHmmss</c> timestamp.
/// </summary>
/// <remarks>
/// <para>
/// Before <c>plans/00001</c> step 4 this was silent twice over: the generator fell back to the clock
/// and reported nothing, while MSBuild derived <c>AutoVersion</c> and <c>AutoPackageVersion</c> from
/// the same bad value, so one input produced two different versions and a package could carry one
/// that disagreed with the assembly inside it.
/// </para>
/// <para>
/// The two halves are deliberately distinguishable: <c>BNAV05</c> is the analyzer rule, and the
/// <c>Build.props</c> target's error carries no code. That is what makes the ungated case below
/// assertable, and it is the case that matters, because a project using
/// <c>$(AutoPackageVersion)</c> without enabling the generator is the one that packs the version.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class StampValidationTests
{
    private const string MSBuildHalf = "AutoVersion and AutoPackageVersion were left empty";

    /// <summary>Every one of these is fourteen characters, so the old length-only guard passed them all.</summary>
    [TestMethod]
    [DataRow("not-a-stamp", DisplayName = "not a number at all")]
    [DataRow("20261345000000", DisplayName = "month 13")]
    [DataRow("20260230000000", DisplayName = "30 February")]
    [DataRow("20260229000000", DisplayName = "29 February in a common year")]
    [DataRow("20260431000000", DisplayName = "31 April")]
    [DataRow("20260105250000", DisplayName = "hour 25")]
    [DataRow("20260105006000", DisplayName = "minute 60")]
    [DataRow("2026010500050X", DisplayName = "fourteen characters, the last not a digit")]
    public void AStampThatDoesNotParse_IsReportedByTheGenerator(string stamp)
    {
        using var fixture = FixtureConsumer.Create();

        var log = fixture.Build("-p:BuildTimestamp=" + stamp);

        StringAssert.Contains(log, "BNAV05", StringComparison.Ordinal,
            $"The generator should report BNAV05 for '{stamp}' rather than falling back to the clock in silence.");

        Assert.IsFalse(
            log.Contains(MSBuildHalf, StringComparison.Ordinal),
            "With the generator enabled, Build.props should stay quiet and leave the report to BNAV05: "
                + "its target runs before the compiler, so an ungated one would fail the build first "
                + "and BNAV05 could never be reached.");
    }

    /// <summary>
    /// Decision 6's whole reason. No generator runs, so <c>BNAV05</c> cannot fire — and this is the
    /// project that would pack the version.
    /// </summary>
    [TestMethod]
    public void WithTheGeneratorDisabled_OnlyMSBuildReports()
    {
        using var fixture = FixtureConsumer.Create();

        var log = fixture.Build(
            "-p:BuildTimestamp=20260230000000",
            "-p:GenerateAutoVersionedAssemblyInfo=false");

        StringAssert.Contains(log, MSBuildHalf, StringComparison.Ordinal,
            "Build.props must report a malformed stamp even with the generator disabled: "
                + "that is the project which consumes AutoPackageVersion.");

        Assert.IsFalse(
            log.Contains("BNAV05", StringComparison.Ordinal),
            "BNAV05 comes from the generator, which does not run here, so it cannot be what reported this.");
    }

    /// <summary>
    /// The assertion that the validation is in the derivation's condition and not only in the
    /// target: <c>-getProperty</c> is an evaluation and runs no target at all.
    /// </summary>
    [TestMethod]
    [DataRow("20260230000000", "", DisplayName = "30 February derives nothing")]
    [DataRow("20261345000000", "", DisplayName = "month 13 derives nothing")]
    [DataRow("2026010500050X", "", DisplayName = "a non-digit derives nothing")]
    [DataRow("20240229000000", "2024.1.229", DisplayName = "29 February 2024 is a real date")]
    [DataRow("20260131000000", "2026.1.131", DisplayName = "31 January is a real date")]
    [DataRow("20260105084500", "2026.1.105", DisplayName = "an ordinary stamp")]
    public void AutoPackageVersion_IsEmptyRatherThanWrong(string stamp, string expected)
    {
        using var fixture = FixtureConsumer.Create();

        Assert.AreEqual(
            expected,
            fixture.Evaluate("AutoPackageVersion", "-p:BuildTimestamp=" + stamp),
            $"Evaluating AutoPackageVersion for '{stamp}' should give '{expected}'. A wrong version is "
                + "worse than none, and only a check in the condition can keep -getProperty honest.");
    }

    /// <summary>
    /// Pins <c>DaysInMonth</c> against the hardcoded 31 someone will reach for: the same day of the
    /// same month is valid in one year and not in the other.
    /// </summary>
    [TestMethod]
    public void TheLeapDay_IsJudgedByTheYear()
    {
        using var fixture = FixtureConsumer.Create();

        Assert.AreEqual(
            "2024.1.229",
            fixture.Evaluate("AutoPackageVersion", "-p:BuildTimestamp=20240229000000"),
            "29 February 2024 is a real date.");

        Assert.AreEqual(
            string.Empty,
            fixture.Evaluate("AutoPackageVersion", "-p:BuildTimestamp=20260229000000"),
            "29 February 2026 is not, and a hardcoded 31 would have accepted it.");
    }

    /// <summary>A well-formed stamp reports neither half, which is what keeps this from crying wolf.</summary>
    [TestMethod]
    public void AWellFormedStamp_ReportsNothing()
    {
        using var fixture = FixtureConsumer.Create();

        var log = fixture.Build("-p:BuildTimestamp=20260105084500");

        Assert.IsFalse(log.Contains("BNAV05", StringComparison.Ordinal), "BNAV05 should not fire for a valid stamp.");
        Assert.IsFalse(log.Contains(MSBuildHalf, StringComparison.Ordinal), "Build.props should derive a version from a valid stamp.");
        Assert.AreEqual("2026.1.105.845", FixtureConsumer.ReadReportedValue(log, "FIXTURE-AUTOVERSION"));
    }
}
