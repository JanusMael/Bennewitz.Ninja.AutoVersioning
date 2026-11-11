using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;
using Bennewitz.Ninja.AutoVersioning.SourceGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests.Architecture;

/// <summary>
/// AssemblyQuality's rules over the generator's compiled assembly, read the way it ships: the shape a
/// consumer's compiler loads. Ported from the bbpkg template's <c>AssemblyQualityTests</c>.
/// </summary>
/// <remarks>
/// Each rule is held to AssemblyQuality's checks: it skipped nothing, it found nothing, and, where
/// the assembly gives it a candidate, it inspected something. Findings alone cannot fail a rule that
/// never looked. BNAQ1001 counts only public methods that take a token, which the generator has none
/// of. BNAQ1003, the layering rule, is left out: it needs a table of which project may reference
/// what, and a one-project repository has no layers.
/// </remarks>
[TestClass]
public sealed class AssemblyQualityTests
{
    private static readonly Assembly Shipped = typeof(BuildVersion).Assembly;

    /// <summary>
    /// BNAQ1005: every grant compiled into the assembly names an assembly the repository's two grant
    /// files allow: the generated list of its own assemblies, and the hand-written External file's
    /// names.
    /// </summary>
    /// <remarks>
    /// Read from the compiled assembly, so a grant from anywhere else (a stray attribute, a project
    /// item, a generator) fails here even when the conventions check cannot see it. BNAQ1005 loads
    /// nothing, so it never fills Skipped, and that assertion is left out.
    /// </remarks>
    [TestMethod]
    public void BNAQ1005_every_grant_names_an_allowed_assembly()
    {
        string[] allowed =
        [
            .. GrantedNames("AssemblyInfo.InternalsVisibleTo.cs"),
            .. GrantedNames("AssemblyInfo.InternalsVisibleTo.External.cs"),
        ];

        AssemblyRuleResult result = new FriendGrantRule(allowed).Analyze(AssemblyScanContext.Of(Shipped));

        Assert.IsTrue(result.Inspected > 0, "BNAQ1005 inspected no grant, so it proved nothing.");
        Assert.IsTrue(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>BNAQ1001: no public method takes a <c>CancellationToken</c> with a default.</summary>
    [TestMethod]
    public void BNAQ1001_no_public_method_takes_a_defaulted_cancellation_token()
    {
        AssemblyRuleResult result = new CancellationTokenRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.IsTrue(result.Skipped.Count == 0, string.Join("\n", result.Skipped));
        Assert.IsTrue(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>
    /// BNAQ1002: no type from a leak-prone namespace appears in the public surface, where every
    /// consumer would come to depend on it.
    /// </summary>
    /// <remarks>
    /// ⚠ This one cannot fail yet, and says so by inspecting nothing: the rule's default namespaces
    /// are serializer DOMs, and the generator references none. When it takes a dependency whose types
    /// should stay inside it, name that dependency's namespace with <c>new SurfaceLeakRule([...])</c>
    /// and assert <c>Inspected &gt; 0</c>, as the other rules here do.
    /// </remarks>
    [TestMethod]
    public void BNAQ1002_no_leak_prone_type_appears_in_the_public_surface()
    {
        AssemblyRuleResult result = new SurfaceLeakRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.IsTrue(result.Skipped.Count == 0, string.Join("\n", result.Skipped));
        Assert.IsTrue(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>
    /// BNAQ1004: no namespace segment shadows a referenced root. A segment named <c>System</c> inside
    /// this assembly would make <c>System.IO</c> resolve to it in every consumer, failing with CS0234.
    /// </summary>
    [TestMethod]
    public void BNAQ1004_no_namespace_segment_shadows_a_referenced_root()
    {
        AssemblyRuleResult result = new NamespaceShadowRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.IsTrue(result.Inspected > 0, "BNAQ1004 inspected nothing, so it proved nothing.");
        Assert.IsTrue(result.Skipped.Count == 0, string.Join("\n", result.Skipped));
        Assert.IsTrue(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>The names a grant file grants to, comment lines left out.</summary>
    private static IEnumerable<string> GrantedNames(string file) =>
        File.ReadLines(Path.Combine(RepoRoot(), file))
            .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .SelectMany(line => Regex.Matches(line, "InternalsVisibleTo\\(\"([^\"]+)\"\\)").Select(m => m.Groups[1].Value));

    /// <summary>
    /// Walks up from the test assembly to the directory holding <c>packages.push</c>, the repository
    /// root, where the grant files are.
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
