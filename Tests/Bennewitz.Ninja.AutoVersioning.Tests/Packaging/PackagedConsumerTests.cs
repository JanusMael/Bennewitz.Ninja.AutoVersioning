using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Bennewitz.Ninja.AutoVersioning.Tests.Incremental;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Packaging;

/// <summary>
/// The whole point of <c>plans/00002</c>, measured end to end: a consumer installs
/// <c>Bennewitz.Ninja.AutoVersioning.Abstractions</c> <b>as a package</b>, the generator stamps that
/// consumer's assembly, and the consumer reads its own stamp back with
/// <c>BuildVersion.TryGetFromFile</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>It consumes the <c>.nupkg</c>, never the built DLL.</b> The defect this plan exists to fix
/// is a <i>packaging</i> defect — <c>TryGetFromFile</c> was always correct, and only the package
/// failed to deliver it. <c>FixtureConsumer</c> wires the generator with
/// <c>&lt;Analyzer Include="…{the built dll}" /&gt;</c>, and a test written the same way here would
/// reference <c>.Abstractions.dll</c> by path, pass, and prove nothing about whether the package
/// delivers the type: it would be a test that cannot fail for the thing it exists to catch. So this
/// packs, writes a <c>NuGet.config</c> whose <b>only</b> source is that directory, and gives the
/// fixture a real <c>PackageReference</c>.
/// </para>
/// <para>
/// ⚠ <b>The package version is unique per run and the restore gets its own packages folder.</b> A
/// fixed version would let NuGet's global cache serve an <i>earlier</i> run's package — one that
/// still carried a <c>lib/</c> — in place of the one under test, and the mutation that is supposed
/// to turn this red would stay green.
/// </para>
/// <para>
/// ⚠ A pack, a restore, a build and a run, each its own process — the cost of exercising the
/// package rather than the project. It is the most expensive test outside <c>Incremental/</c>, and
/// all nine <c>Packaging/</c> tests together still run in 3.6 s.
/// </para>
/// <para>
/// ⚠ <c>DoNotParallelize</c> because this class and
/// <c>PackagingTests.The_abstractions_package_carries_a_lib_and_a_readme</c> both run
/// <c>dotnet pack</c> over the <b>same</b> project: different output directories, but one
/// <c>Abstractions/obj</c> between them. Two concurrent packs would race there, and intermittently.
/// This assembly declares no <c>[assembly: Parallelize]</c>, so today they are sequential anyway —
/// the attribute is what keeps that true if one is ever added.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class PackagedConsumerTests
{
    private const string TargetFramework = "net10.0";
    private const string Configuration = "Release";

    /// <summary>
    /// Packs <c>.Abstractions</c> into a local feed, restores a one-project consumer against that
    /// feed alone, builds it with the generator loaded, runs it, and compares the stamp it read back
    /// with the <c>AutoVersion</c> MSBuild derived during that same build.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two values come from one evaluation of one build, so they are comparable exactly: the
    /// report is written by a target in the generated <c>Directory.Build.props</c>, and the stamp is
    /// read out of the Win32 version resource of the assembly that build produced. The consumer is
    /// run <b>without rebuilding</b> for the same reason — a Release build takes a fresh
    /// <c>BuildTimestamp</c> every time, so a rebuild would restamp the assembly and compare two
    /// different builds.
    /// </para>
    /// <para>
    /// ⛔ <b>Red when the package carries no <c>lib/</c>.</b> Proven 2026-11-19 by packing with
    /// <c>-p:IncludeBuildOutput=false -p:NoWarn=NU5128</c>, which is what empties an SDK-packed
    /// library: the consumer failed to build with <c>CS0246 — The type or namespace name
    /// 'BuildVersion' could not be found</c>, which is precisely the consumer-visible defect
    /// <c>plans/00002</c> exists to fix.
    /// </para>
    /// <para>
    /// ⭐ <b>And the comparison is not vacuous</b>, which a two-sided equality always risks. Proven
    /// 2026-11-19 by building the fixture with <c>-p:BuildTimestamp=20260102030405</c>: the report
    /// read <c>2026.1.102.304</c> and the consumer read the same value back out of the assembly, so
    /// both sides follow a stamp the test chose rather than agreeing on whatever the clock gave.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void A_consumer_restoring_the_package_reads_its_own_stamp()
    {
        string work = Path.Combine(
            Path.GetTempPath(),
            "bnav-consumer-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8]);

        string feed = Path.Combine(work, "feed");
        string cache = Path.Combine(work, "cache");
        string consumer = Path.Combine(work, "consumer");
        string version = "0.0.0-fixture-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8];

        try
        {
            Directory.CreateDirectory(feed);
            string packLog = PackagingTests.Pack(AbstractionsProject, feed, "-p:Version=" + version);

            string[] packed = Directory.GetFiles(feed, "*.nupkg");
            Assert.AreEqual(
                1,
                packed.Length,
                $"Expected exactly one .nupkg in the fixture feed. dotnet pack said:{Environment.NewLine}{packLog}");

            WriteConsumer(consumer, feed, cache, version);

            (int buildExit, string buildLog) = RunDotnet(
                consumer, "build", "-c", Configuration, "--nologo", "-nodeReuse:false");

            Assert.AreEqual(
                0,
                buildExit,
                "The consumer did not build against the packaged library. CS0246 on BuildVersion "
                + "means the package delivered no lib/ a consumer can compile against, which is the "
                + $"defect plans/00002 exists to fix. dotnet build said:{Environment.NewLine}{buildLog}");

            // Written by the report target in the Directory.Build.props below, so it is what MSBuild
            // derived during this very build rather than a value recomputed from the clock here.
            string expected = FixtureConsumer.ReadReportedValue(buildLog, "FIXTURE-AUTOVERSION");
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(expected),
                "The consumer's build derived no AutoVersion, so there is no stamp to read back and "
                + $"the comparison below would pass on two empty strings. The build said:{Environment.NewLine}{buildLog}");

            string output = Path.Combine(consumer, "bin", Configuration, TargetFramework);
            string library = Path.Combine(output, "Bennewitz.Ninja.AutoVersioning.Abstractions.dll");
            Assert.IsTrue(
                File.Exists(library),
                "The packaged library did not reach the consumer's output directory, so it is not a "
                + "runtime reference — which is what developmentDependency would do to it. "
                + $"'{output}' holds: {Describe(output)}");

            string application = Path.Combine(output, "Consumer.dll");
            Assert.IsTrue(File.Exists(application), $"The consumer was not built to '{application}'. '{output}' holds: {Describe(output)}");

            // ⚠ Run the built assembly directly: `dotnet run` would re-evaluate the project, take a
            // new Release BuildTimestamp, restamp the assembly, and compare two different builds.
            (int runExit, string runLog) = RunDotnet(consumer, application);

            Assert.AreEqual(
                0,
                runExit,
                "The consumer ran but could not read its own stamp back. Its exit code is 1 when "
                + $"TryGetFromFile declines the assembly this build produced. It said:{Environment.NewLine}{runLog}");

            string actual = FixtureConsumer.ReadReportedValue(runLog, "CONSUMER-STAMP");
            Assert.AreEqual(
                expected,
                actual,
                "The stamp the consumer read back through the package is not the AutoVersion its own "
                + $"build derived. It said:{Environment.NewLine}{runLog}");

            // FuzzyBuildVersionComparer became public API again on 2026-11-19. Nothing else proves a
            // consumer can reach it, the rest of its tests calling it from inside this repository.
            Assert.AreEqual(
                bool.TrueString,
                FixtureConsumer.ReadReportedValue(runLog, "CONSUMER-COMPARER"),
                "The consumer could not use FuzzyBuildVersionComparer, which the package exposes as "
                + $"public API. It said:{Environment.NewLine}{runLog}");
        }
        finally
        {
            Remove(work);
        }
    }

    /// <summary>
    /// Copies the fixture out of the repository and completes it: the templates renamed, the feed
    /// and its own packages folder declared, and the two files NuGet would import written with the
    /// absolute paths this test run has.
    /// </summary>
    private static void WriteConsumer(string consumer, string feed, string cache, string version)
    {
        FixtureConsumer.CopyDirectory(FixtureSource, consumer);

        foreach (string template in Directory.GetFiles(consumer, "*.template", SearchOption.AllDirectories))
        {
            File.Move(template, template[..^".template".Length]);
        }

        // The fixture's own AI-facing files are documentation, not part of a consumer.
        foreach (string name in new[] { "AGENTS.md", "CLAUDE.md" })
        {
            string path = Path.Combine(consumer, name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        File.WriteAllText(Path.Combine(consumer, "NuGet.config"), NuGetConfig(feed, cache));
        File.WriteAllText(Path.Combine(consumer, "Directory.Build.props"), DirectoryBuildProps(version));
        File.WriteAllText(Path.Combine(consumer, "Directory.Build.targets"), DirectoryBuildTargets());
    }

    /// <summary>
    /// The feed as the consumer's only source, and a packages folder of its own.
    /// </summary>
    /// <remarks>
    /// ⛔ <c>&lt;clear /&gt;</c> drops every source the machine's own configuration would otherwise
    /// contribute, nuget.org among them: the package under test must come from the directory this
    /// run packed and from nowhere else. <c>globalPackagesFolder</c> is the other half — without it
    /// the extracted package would land in the machine's shared cache, where it would outlive the
    /// run and could be served back to a later one.
    /// </remarks>
    private static string NuGetConfig(string feed, string cache) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="fixture-feed" value="{feed}" />
          </packageSources>
          <config>
            <add key="globalPackagesFolder" value="{cache}" />
          </config>
        </configuration>
        """;

    /// <summary>
    /// What NuGet imports from the AutoVersioning package, plus the generator this test run built
    /// and the version of the package the fixture is to restore.
    /// </summary>
    /// <remarks>
    /// The generator is referenced by path, as <c>FixtureConsumer</c> does and as a consumer's
    /// compiler loads it: an <c>Analyzer</c> item contributes nothing to the compilation's reference
    /// set, so it cannot be what supplies <c>BuildVersion</c> to the fixture. The package is the only
    /// candidate, which is what makes this test able to fail.
    /// </remarks>
    private static string DirectoryBuildProps(string version) =>
        $"""
        <Project>

          <!-- Where NuGet imports this package's props: before the SDK's Configuration default. -->
          <Import Project="{Path.Combine(FixtureConsumer.RepositoryRoot, "Build.props")}" />

          <PropertyGroup>
            <GenerateAutoVersionedAssemblyInfo>true</GenerateAutoVersionedAssemblyInfo>
            <AssemblyCompany>FixtureCo</AssemblyCompany>
            <AssemblyProduct>FixtureProduct</AssemblyProduct>

            <!-- Unique per run: NuGet's cache must never serve an earlier run's package. -->
            <AbstractionsPackageVersion>{version}</AbstractionsPackageVersion>
          </PropertyGroup>

          <ItemGroup>
            <Analyzer Include="{FixtureConsumer.GeneratorAssembly}" />
          </ItemGroup>

          <Target Name="ReportFixtureProperties" BeforeTargets="CoreCompile">
            <Message Importance="High" Text="FIXTURE-AUTOVERSION=[$(AutoVersion)]" />
          </Target>

        </Project>
        """;

    private static string DirectoryBuildTargets() =>
        $"""
        <Project>

          <!-- Where NuGet imports this package's targets: after the project body. -->
          <Import Project="{Path.Combine(FixtureConsumer.RepositoryRoot, "Build.targets")}" />

        </Project>
        """;

    /// <summary>Runs <c>dotnet</c> and answers its exit code with everything it wrote.</summary>
    private static (int ExitCode, string Output) RunDotnet(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // A reused node can hold a handle on the temp tree this test deletes, and a stale evaluation
        // of Build.props, which the fixture imports.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("dotnet did not start.");

        string output = process.StandardOutput.ReadToEnd() + Environment.NewLine + process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output);
    }

    private static string AbstractionsProject
    {
        get
        {
            string path = Path.Combine(
                FixtureConsumer.RepositoryRoot, "Abstractions", "Bennewitz.Ninja.AutoVersioning.Abstractions.csproj");

            Assert.IsTrue(File.Exists(path), $"The .Abstractions project is not at '{path}'.");
            return path;
        }
    }

    private static string FixtureSource
    {
        get
        {
            string path = Path.Combine(FixtureConsumer.RepositoryRoot, "Tests", "fixtures", "consumer");

            Assert.IsTrue(Directory.Exists(path), $"The consumer fixture is missing from '{path}'.");
            return path;
        }
    }

    /// <summary>Names what a directory holds, so a missing file fails with the reason rather than the symptom.</summary>
    private static string Describe(string directory) =>
        Directory.Exists(directory)
            ? string.Join(", ", Directory.GetFiles(directory).Select(Path.GetFileName).Order())
            : "nothing — the directory does not exist";

    private static void Remove(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A temp directory left behind is not a failure.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
