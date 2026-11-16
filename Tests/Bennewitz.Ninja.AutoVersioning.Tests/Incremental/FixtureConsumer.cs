using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bennewitz.Ninja.AutoVersioning.SourceGenerators;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Incremental;

/// <summary>
/// A copy of <c>Tests/fixtures/incremental</c> outside the repository, completed and built the way
/// NuGet would arrange this package for a consumer, with each project's <c>CoreCompile</c> decision
/// read back out of MSBuild's own detailed log.
/// </summary>
/// <remarks>
/// The copy has to leave the repository: the root <c>Directory.Build.props</c>,
/// <c>Directory.Packages.props</c> and <c>Directory.Build.targets</c> would otherwise reach it, and
/// the fixture would measure this repository's build rather than a consumer's.
/// </remarks>
internal sealed class FixtureConsumer : IDisposable
{
    /// <summary>Every project in the fixture, by the name MSBuild logs it under.</summary>
    internal static readonly string[] AllProjects = ["Core.csproj", "Model.csproj", "App.csproj", "Leaf.csproj"];

    private FixtureConsumer(string root) => Root = root;

    internal string Root { get; }

    internal string Solution => Path.Combine(Root, "incremental.slnx");

    internal string LeafProject => Path.Combine(Root, "src", "Leaf", "Leaf.csproj");

    /// <summary>
    /// Copies the fixture out, renames every <c>.template</c> away, and writes the two files NuGet
    /// would import: the props where <c>Microsoft.Common.props</c> reads them, before the SDK gives
    /// <c>Configuration</c> its default, and the targets after the project body.
    /// </summary>
    internal static FixtureConsumer Create()
    {
        var destination = Path.Combine(
            Path.GetTempPath(),
            "bnav-incremental-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8]);

        CopyDirectory(FixtureSource, destination);

        foreach (var template in Directory.GetFiles(destination, "*.template", SearchOption.AllDirectories))
        {
            File.Move(template, template[..^".template".Length]);
        }

        // The fixture's own AI-facing files are documentation, not part of a consumer.
        foreach (var name in new[] { "AGENTS.md", "CLAUDE.md" })
        {
            var path = Path.Combine(destination, name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        File.WriteAllText(Path.Combine(destination, "Directory.Build.props"), DirectoryBuildProps());
        File.WriteAllText(Path.Combine(destination, "Directory.Build.targets"), DirectoryBuildTargets());

        return new FixtureConsumer(destination);
    }

    /// <summary>
    /// Runs <c>dotnet</c> over the fixture and returns everything it wrote. Node reuse is off so no
    /// state survives between builds, and a single node keeps the log sequential, which is what makes
    /// a decision attributable to the project whose header preceded it.
    /// </summary>
    internal string Run(string verb, string target, params string[] extraArguments)
    {
        var arguments = new List<string> { verb, target, "-v:detailed", "-nodeReuse:false", "-m:1" };
        arguments.AddRange(extraArguments);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // MSBUILDDISABLENODEREUSE belts the -nodeReuse:false braces; a reused node can hold a stale
        // evaluation of Build.props, which is exactly the file under test.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("dotnet did not start.");

        var output = new StringBuilder();
        output.Append(process.StandardOutput.ReadToEnd());
        output.Append(process.StandardError.ReadToEnd());
        process.WaitForExit();

        return output.ToString();
    }

    internal string Build(params string[] extraArguments) => Run("build", Solution, extraArguments);

    /// <summary>
    /// The projects MSBuild compiled, and every project it reached a <c>CoreCompile</c> decision for.
    /// </summary>
    /// <remarks>
    /// Two line shapes carry the decision, neither of which names its project:
    /// <c>Building target "CoreCompile" completely.</c> and
    /// <c>Target "CoreCompile" skipped. Previously built successfully.</c> The project comes from the
    /// header above them, so the log is read in order. A project can be asked twice in one build,
    /// once per request, which is why this answers in sets rather than counts: what matters is
    /// whether a project compiled at all.
    /// </remarks>
    internal static (HashSet<string> Compiled, HashSet<string> Decided) ReadCompileDecisions(string log)
    {
        // No closing quote in the pattern: [^"]+ already stops at one, and a trailing quote would
        // collide with the raw string's own delimiter.
        var header = new Regex(
            """Target "CoreCompile" in file ".*" from project "(?<project>[^"]+)""",
            RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

        var compiled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var decided = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = (string?)null;

        foreach (var line in log.Split('\n'))
        {
            var match = header.Match(line);
            if (match.Success)
            {
                current = Path.GetFileName(match.Groups["project"].Value);
                continue;
            }

            if (current is null)
            {
                continue;
            }

            if (line.Contains("Building target \"CoreCompile\" completely", StringComparison.Ordinal))
            {
                compiled.Add(current);
                decided.Add(current);
            }
            else if (line.Contains("Target \"CoreCompile\" skipped", StringComparison.Ordinal))
            {
                decided.Add(current);
            }
        }

        return (compiled, decided);
    }

    /// <summary>Reads the value a report target printed, so a missing report fails rather than reading as empty.</summary>
    internal static string ReadReportedValue(string log, string key)
    {
        var match = Regex.Match(
            log,
            Regex.Escape(key) + @"=\[(?<value>[^\]]*)\]",
            RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

        return match.Success
            ? match.Groups["value"].Value
            : throw new InvalidOperationException($"The fixture never reported '{key}'. The build output was:{Environment.NewLine}{log}");
    }

    private static string FixtureSource
    {
        get
        {
            var path = Path.Combine(RepositoryRoot, "Tests", "fixtures", "incremental");
            return Directory.Exists(path)
                ? path
                : throw new DirectoryNotFoundException($"The incremental fixture is missing from '{path}'.");
        }
    }

    /// <summary>Walks up from the test assembly until the generator's own project file appears.</summary>
    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Bennewitz.Ninja.AutoVersioning.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Walked up from '{AppContext.BaseDirectory}' without finding Bennewitz.Ninja.AutoVersioning.csproj.");
        }
    }

    /// <summary>The generator this test run built, loaded by the fixture's compiler as a consumer would.</summary>
    private static string GeneratorAssembly =>
        typeof(AssemblyInfoGenerator).Assembly.Location;

    private static string DirectoryBuildProps() =>
        $"""
        <Project>

          <!-- Where NuGet imports this package's props: before the SDK's Configuration default. -->
          <Import Project="{Path.Combine(RepositoryRoot, "Build.props")}" />

          <PropertyGroup>
            <!-- Captured here, so it is what Build.props itself saw rather than the SDK's default. -->
            <ConfigurationAtPackagePropsTime>$(Configuration)</ConfigurationAtPackagePropsTime>

            <GenerateAutoVersionedAssemblyInfo>true</GenerateAutoVersionedAssemblyInfo>
            <AssemblyCompany>FixtureCo</AssemblyCompany>
            <AssemblyProduct>FixtureProduct</AssemblyProduct>
          </PropertyGroup>

          <ItemGroup>
            <Analyzer Include="{GeneratorAssembly}" />
          </ItemGroup>

          <Target Name="ReportFixtureProperties" BeforeTargets="CoreCompile">
            <Message Importance="High" Text="FIXTURE-CONFIGURATION=[$(ConfigurationAtPackagePropsTime)]" />
            <Message Importance="High" Text="FIXTURE-STAMP=[$(BuildTimestamp)]" />
            <Message Importance="High" Text="FIXTURE-AUTOVERSION=[$(AutoVersion)]" />
            <Message Importance="High" Text="FIXTURE-AUTOPACKAGEVERSION=[$(AutoPackageVersion)]" />
          </Target>

        </Project>
        """;

    private static string DirectoryBuildTargets() =>
        $"""
        <Project>

          <!-- Where NuGet imports this package's targets: after the project body. -->
          <Import Project="{Path.Combine(RepositoryRoot, "Build.targets")}" />

        </Project>
        """;

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A build server can still hold a handle. A temp directory left behind is not a failure.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
