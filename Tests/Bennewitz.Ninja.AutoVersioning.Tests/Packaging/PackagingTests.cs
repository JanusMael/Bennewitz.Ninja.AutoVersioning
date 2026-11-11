using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Packaging;

/// <summary>
/// Guards what this repository publishes: that every packable project is deliberately classified,
/// and that no release step globs. Ported from the bbpkg template's <c>PackagingTests</c>, with the
/// projects read from this repository's layout: the package at the root, the tests under
/// <c>Tests/</c>, and no solution file.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>A published NuGet version can never be replaced</b>, only unlisted. Everything else here
/// is recoverable; this is not. So the guard runs on every push rather than at release time, when
/// finding out is still free.
/// </para>
/// <para>
/// ⚠ <b>The argument this exists to refuse is "it is fine, every packed id is pushable."</b> That
/// is true of today's project list and of no other. <c>Bennewitz.Ninja.DiffView</c> came within one
/// project of publishing a deliberately private package because a glob swept it up.
/// </para>
/// </remarks>
[TestClass]
public sealed class PackagingTests
{
    /// <summary>
    /// Every packable project appears in <c>packages.push</c> or <c>packages.local</c>. Adding one
    /// and classifying it nowhere fails here rather than at the push.
    /// </summary>
    [TestMethod]
    public void Every_packable_project_is_classified()
    {
        HashSet<string> packable = PackableIds();
        HashSet<string> classified = [.. Ids("packages.push"), .. Ids("packages.local")];

        Assert.IsTrue(packable.Count > 0, "No packable project was found, so nothing here was checked.");

        string[] unclassified = [.. packable.Except(classified).Order()];
        Assert.IsTrue(
            unclassified.Length == 0,
            "Packable but in neither packages.push nor packages.local: " + string.Join(", ", unclassified) +
            ". Classify it: publishing it by accident cannot be undone.");

        string[] phantom = [.. classified.Except(packable).Order()];
        Assert.IsTrue(
            phantom.Length == 0,
            "Listed but nothing packs it: " + string.Join(", ", phantom) +
            ". A stale entry means the push names a package that will not exist.");
    }

    /// <summary>
    /// No id is in both lists, which would say "publish this" and "never publish this" at once.
    /// </summary>
    [TestMethod]
    public void No_id_is_both_published_and_private()
    {
        string[] both = [.. Ids("packages.push").Intersect(Ids("packages.local")).Order()];

        Assert.IsTrue(both.Length == 0, "In both packages.push and packages.local: " + string.Join(", ", both));
    }

    /// <summary>
    /// No release step globs, and both publishing steps take their list from what
    /// <c>scripts/release-target.cs</c> resolves, which reads <c>packages.push</c>, rather than
    /// repeating it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The id is deliberately NOT asserted literally here.</b> A workflow that repeats the list
    /// has two homes for it, and the second one silently rots when a package is added. Driving both
    /// steps from the declaration means the list has exactly one home, and <c>assert-packages</c>
    /// closes the loop by checking the packed output against that same file. <c>gh release
    /// create</c> is covered as well as <c>dotnet nuget push</c>: a glob there never announces
    /// itself the way a permanent nuget.org id does, it just ships something nobody chose.
    /// </remarks>
    [TestMethod]
    public void The_release_workflow_globs_nothing_and_publishes_what_is_declared()
    {
        string workflow = ReleaseWorkflow();

        Assert.IsFalse(workflow.Contains("*.nupkg", StringComparison.Ordinal), "The release workflow globs *.nupkg.");
        Assert.IsTrue(
            workflow.Contains("scripts/release-target.cs -- resolve", StringComparison.Ordinal),
            "The release workflow does not resolve what it publishes through scripts/release-target.cs.");

        int idReads = workflow.Split("read -ra ids <<< \"$IDS\"").Length - 1;
        Assert.IsTrue(
            idReads == 2,
            $"The push and the GitHub release should each read the resolved IDS; found {idReads}. " +
            "A step that stops reading them stops publishing what is declared.");
    }

    /// <summary>
    /// Nothing in <c>packages.local</c> is named anywhere in the release workflow: a private
    /// package should have no path to a publishing step at all.
    /// </summary>
    [TestMethod]
    public void The_release_workflow_never_names_a_private_package()
    {
        string workflow = ReleaseWorkflow();

        foreach (string id in Ids("packages.local"))
        {
            Assert.IsFalse(workflow.Contains(id, StringComparison.Ordinal), $"The release workflow names the private package {id}.");
        }
    }

    /// <summary>
    /// Both publishing steps still exist. Without this the test above passes vacuously against a
    /// workflow that has stopped publishing at all.
    /// </summary>
    [TestMethod]
    public void The_release_workflow_still_has_both_publishing_steps()
    {
        string workflow = ReleaseWorkflow();

        Assert.IsTrue(workflow.Contains("dotnet nuget push", StringComparison.Ordinal), "The release workflow no longer pushes.");
        Assert.IsTrue(workflow.Contains("gh release create", StringComparison.Ordinal), "The release workflow no longer creates a GitHub release.");
    }

    /// <summary>
    /// The preflight exists, so the credentials are exercisable before a tag makes anything
    /// permanent.
    /// </summary>
    [TestMethod]
    public void The_release_workflow_has_a_credential_preflight()
    {
        string workflow = ReleaseWorkflow();

        Assert.IsTrue(workflow.Contains("RELEASING", StringComparison.Ordinal), "The release workflow has no RELEASING gate.");
        Assert.IsTrue(workflow.Contains("NuGet/login", StringComparison.Ordinal), "The release workflow has no trusted-publishing login.");
    }

    /// <summary>
    /// Comment lines are dropped before searching, because the comments above the publishing steps
    /// explain the very glob these tests forbid: a whole-file search would fail on the explanation.
    /// </summary>
    private static string ReleaseWorkflow()
    {
        string path = Path.Combine(RepoRoot(), ".github", "workflows", "release.yml");
        Assert.IsTrue(File.Exists(path), "There is no release workflow, so nothing publishes, and nothing gates what would.");

        return string.Join('\n', File.ReadLines(path).Where(line => !line.TrimStart().StartsWith('#')));
    }

    /// <summary>
    /// Reads one of the two package lists, dropping blank lines and <c>#</c> comments.
    /// </summary>
    private static HashSet<string> Ids(string fileName)
    {
        string path = Path.Combine(RepoRoot(), fileName);
        Assert.IsTrue(File.Exists(path), fileName + " is missing, so nothing declares what may be published.");

        return
        [
            .. File.ReadLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
        ];
    }

    /// <summary>
    /// Package ids of every packable project, read from the project files rather than from packed
    /// output, so the guard runs before anything has been packed: the projects at the root and one
    /// level under <c>Tests/</c>, which is every project this repository has.
    /// </summary>
    /// <remarks>
    /// ⚠ <c>PackageId</c> falls back to <c>AssemblyName</c> and then the project file name when it
    /// is not set, which is how a project acquires an id nobody chose. Mirrored here so an
    /// unclassified project cannot hide behind an unset property. A <c>NuspecFile</c> overrides all
    /// three, so its <c>&lt;id&gt;</c> is read instead.
    /// </remarks>
    private static HashSet<string> PackableIds()
    {
        string root = RepoRoot();
        string tests = Path.Combine(root, "Tests");
        HashSet<string> ids = [];

        IEnumerable<string> projects = Directory.EnumerateFiles(root, "*.csproj")
            .Concat(Directory.EnumerateDirectories(tests).SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj")));

        foreach (string project in projects)
        {
            XDocument document = XDocument.Load(project);

            // ⛔ Fails closed: a project is packable unless it says otherwise, because the SDK packs a
            // class library by default. The last declaration wins, as it does in MSBuild.
            bool packable = !string.Equals(
                document.Descendants("IsPackable").LastOrDefault()?.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase);

            if (!packable)
            {
                continue;
            }

            // ⚠ A project that packs through a NuspecFile takes its id from the nuspec's <id>, which
            // wins over every property; this repository's package does. Read where pack reads it.
            string? nuspec = document.Descendants("NuspecFile").LastOrDefault()?.Value.Trim();
            if (!string.IsNullOrWhiteSpace(nuspec))
            {
                string nuspecPath = Path.Combine(Path.GetDirectoryName(project)!, nuspec);
                Assert.IsTrue(File.Exists(nuspecPath), $"{Path.GetFileName(project)} packs through {nuspec}, which does not exist.");
                string? id = XDocument.Load(nuspecPath).Descendants().FirstOrDefault(e => e.Name.LocalName == "id")?.Value.Trim();
                Assert.IsFalse(string.IsNullOrWhiteSpace(id), $"{nuspec} declares no <id>.");
                ids.Add(id!);
                continue;
            }

            string? declared = document.Descendants("PackageId").LastOrDefault()?.Value.Trim();
            string? assembly = document.Descendants("AssemblyName").LastOrDefault()?.Value.Trim();

            ids.Add(Pick(declared, assembly, Path.GetFileNameWithoutExtension(project)));
        }

        return ids;

        static string Pick(string? declared, string? assembly, string fallback) =>
            !string.IsNullOrWhiteSpace(declared) ? declared
            : !string.IsNullOrWhiteSpace(assembly) ? assembly
            : fallback;
    }

    /// <summary>
    /// Walks up from the test assembly to the directory holding <c>packages.push</c>, so the tests do
    /// not depend on the working directory a runner happens to choose.
    /// </summary>
    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "packages.push")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "No directory above the test assembly holds packages.push.");
        return directory.FullName;
    }
}
