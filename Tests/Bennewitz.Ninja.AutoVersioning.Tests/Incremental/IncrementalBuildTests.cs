using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Incremental;

/// <summary>
/// What a consumer's incremental build pays for the stamp, measured as MSBuild's own
/// <c>CoreCompile</c> decisions over a real four-project consumer.
/// </summary>
/// <remarks>
/// <para>
/// This is the only test that exercises <c>Build.props</c> and <c>Build.targets</c> at all: the
/// behaviour lives in the order MSBuild evaluates files in, which nothing but a build shows. It is
/// also the only guard on <c>plans/00001</c>'s invariant, that a Debug build holds its stamp to its
/// day and every other build takes one.
/// </para>
/// <para>
/// Every assertion that counts compiles passes <c>-p:BuildTimestamp</c> or relies on the day, never
/// on the clock, so no case can straddle a minute or a midnight and read as a real recompile.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class IncrementalBuildTests
{
    /// <summary>
    /// The case the plan exists for. Nothing changed between two Debug builds, so nothing should
    /// compile; today every project does, because each took a new stamp a second apart.
    /// </summary>
    [TestMethod]
    public void SecondDebugBuild_OfAnUnchangedTree_CompilesNothing()
    {
        using var fixture = FixtureConsumer.Create();

        fixture.Build();
        var log = fixture.Build();

        AssertEveryProjectWasDecided(log);
        var (compiled, _) = FixtureConsumer.ReadCompileDecisions(log);

        Assert.AreEqual(
            0,
            compiled.Count,
            "An unchanged Debug build should compile nothing. It compiled: " + string.Join(", ", compiled.Order()));
    }

    /// <summary>
    /// A body edit in the library the other three reference must compile that one project. Today it
    /// compiles all four, because the new stamp makes a new reference assembly.
    /// </summary>
    [TestMethod]
    public void DebugBuild_AfterABodyEditInTheReferencedLibrary_CompilesOnlyThatProject()
    {
        using var fixture = FixtureConsumer.Create();

        fixture.Build();
        EditMethodBody(Path.Combine(fixture.Root, "src", "Core", "Numbers.cs"));
        var log = fixture.Build();

        AssertEveryProjectWasDecided(log);
        var (compiled, _) = FixtureConsumer.ReadCompileDecisions(log);

        CollectionAssert.AreEquivalent(
            new[] { "Core.csproj" },
            compiled.ToArray(),
            "Only the edited project should compile. It compiled: " + string.Join(", ", compiled.Order()));
    }

    /// <summary>A body edit in the project nothing references must compile exactly that project.</summary>
    [TestMethod]
    public void DebugBuild_AfterABodyEditInTheUnreferencedProject_CompilesOnlyThatProject()
    {
        using var fixture = FixtureConsumer.Create();

        fixture.Build();
        EditMethodBody(Path.Combine(fixture.Root, "src", "Leaf", "Helper.cs"));
        var log = fixture.Build();

        AssertEveryProjectWasDecided(log);
        var (compiled, _) = FixtureConsumer.ReadCompileDecisions(log);

        CollectionAssert.AreEquivalent(
            new[] { "Leaf.csproj" },
            compiled.ToArray(),
            "Only the edited project should compile. It compiled: " + string.Join(", ", compiled.Order()));
    }

    /// <summary>
    /// Release keeps a stamp per build, deliberately: it is the build that leaves the machine, from a
    /// clean checkout that compiles everything anyway. This pins that the hold did not reach it.
    /// </summary>
    [TestMethod]
    public void SecondReleaseBuild_OfAnUnchangedTree_CompilesEverything()
    {
        using var fixture = FixtureConsumer.Create();

        fixture.Build("-c", "Release");
        var log = fixture.Build("-c", "Release");

        AssertEveryProjectWasDecided(log);
        var (compiled, _) = FixtureConsumer.ReadCompileDecisions(log);

        Assert.AreEqual(
            FixtureConsumer.AllProjects.Length,
            compiled.Count,
            "A Release build takes a stamp per build, so every project should compile. It compiled: "
                + string.Join(", ", compiled.Order()));
    }

    /// <summary>
    /// The trap decision 2 rests on: building one project with no <c>-c</c> reaches
    /// <c>Build.props</c> with <c>Configuration</c> empty, so a hold written as <c>== 'Debug'</c>
    /// alone would miss it.
    /// </summary>
    [TestMethod]
    public void SingleProjectDebugBuild_WithNoConfiguration_CompilesNothing()
    {
        using var fixture = FixtureConsumer.Create();

        fixture.Run("build", fixture.LeafProject);
        var log = fixture.Run("build", fixture.LeafProject);

        var (compiled, decided) = FixtureConsumer.ReadCompileDecisions(log);

        Assert.AreEqual(
            string.Empty,
            FixtureConsumer.ReadReportedValue(log, "FIXTURE-CONFIGURATION"),
            "Building one project with no -c should reach Build.props with Configuration empty.");

        Assert.AreEqual(
            3,
            decided.Count,
            "Leaf and the two projects it references should each reach a decision. Decided: "
                + string.Join(", ", decided.Order()));

        Assert.AreEqual(
            0,
            compiled.Count,
            "An unchanged Debug build of one project should compile nothing. It compiled: "
                + string.Join(", ", compiled.Order()));
    }

    /// <summary>
    /// Decision 4: a stamp set before <c>Build.props</c> still wins, which is how a consumer opts
    /// back into a stamp per Debug build.
    /// </summary>
    [TestMethod]
    public void AStampGivenOnTheCommandLine_IsTheOneUsed()
    {
        using var fixture = FixtureConsumer.Create();

        var log = fixture.Build("-p:BuildTimestamp=20260105084500");

        Assert.AreEqual("20260105084500", FixtureConsumer.ReadReportedValue(log, "FIXTURE-STAMP"));
        Assert.AreEqual("2026.1.105.845", FixtureConsumer.ReadReportedValue(log, "FIXTURE-AUTOVERSION"));
        Assert.AreEqual("2026.1.105", FixtureConsumer.ReadReportedValue(log, "FIXTURE-AUTOPACKAGEVERSION"));
    }

    /// <summary>
    /// What <c>Configuration</c> reaches the package props for each command that does not pass
    /// <c>-c</c>. Decision 2 rests on these values, and reading them from the project body instead
    /// reports <c>Debug</c> for every one, which measures the SDK's default and nothing about this.
    /// </summary>
    [TestMethod]
    [DataRow("build", false, "", DisplayName = "dotnet build, one project - empty")]
    [DataRow("build", true, "Debug", DisplayName = "dotnet build, a solution - Debug")]
    [DataRow("pack", false, "Release", DisplayName = "dotnet pack, one project - Release")]
    [DataRow("pack", true, "Release", DisplayName = "dotnet pack, a solution - Release")]
    [DataRow("publish", false, "Release", DisplayName = "dotnet publish, one project - Release")]
    public void ConfigurationAtPackagePropsTime_PerCommand(string verb, bool wholeSolution, string expected)
    {
        using var fixture = FixtureConsumer.Create();

        var log = fixture.Run(verb, wholeSolution ? fixture.Solution : fixture.LeafProject);

        Assert.AreEqual(
            expected,
            FixtureConsumer.ReadReportedValue(log, "FIXTURE-CONFIGURATION"),
            $"dotnet {verb} with no -c over {(wholeSolution ? "a solution" : "one project")} "
                + $"should reach Build.props with Configuration '{expected}'.");
    }

    /// <summary>
    /// The guard the plan asks for: a log this test could not read must fail, rather than pass as
    /// though nothing compiled.
    /// </summary>
    private static void AssertEveryProjectWasDecided(string log)
    {
        var (_, decided) = FixtureConsumer.ReadCompileDecisions(log);

        CollectionAssert.AreEquivalent(
            FixtureConsumer.AllProjects,
            decided.ToArray(),
            "Every project should reach a CoreCompile decision this test can read. Decided: "
                + string.Join(", ", decided.Order())
                + ". A log that cannot be read must fail rather than read as nothing compiled.");
    }

    /// <summary>Changes a method body and nothing else: no public surface, so no new reference assembly.</summary>
    private static void EditMethodBody(string path)
    {
        var source = File.ReadAllText(path);
        var edited = source.Replace("value * 2", "value + value", StringComparison.Ordinal)
                           .Replace("Model.Record.Twice(Core.Numbers.Double(value))", "Core.Numbers.Double(Model.Record.Twice(value))", StringComparison.Ordinal);

        Assert.AreNotEqual(source, edited, $"The edit to '{path}' changed nothing, so the test would measure the unchanged case.");
        File.WriteAllText(path, edited);
    }
}
