using System;
using System.Globalization;
using System.Linq;
using Bennewitz.Ninja.AutoVersioning.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests;

/// <summary>
/// Guards the text and the character set of everything <see cref="AssemblyInfoTemplate" /> emits.
/// </summary>
/// <remarks>
/// The U+2665 in the build name is deliberate branding. It is stored as UTF-16 in the Win32
/// ProductVersion resource and round-trips intact, comparing equal to the runtime
/// AssemblyInformationalVersion attribute, so it is allowed through by name. Every OTHER character
/// above U+007E is rejected: an unintended one is either a typo or mojibake, and this assembly's
/// version strings surface on consumers' public diagnostic endpoints.
///
/// The heart is written as the escape <c>\u2665</c> throughout, never as a literal character, so
/// this file stays pure ASCII and cannot itself be broken by the class of mishap it exists to catch.
/// </remarks>
[TestClass]
public sealed class AssemblyInfoTemplateTests
{
    /// <summary>U+0020, the lowest printable ASCII character.</summary>
    private const char FirstPrintableAscii = ' ';

    /// <summary>U+007E, the highest printable ASCII character.</summary>
    private const char LastPrintableAscii = '~';

    /// <summary>U+2665 BLACK HEART SUIT: the one deliberate exception to the ASCII rule.</summary>
    private const char Heart = '\u2665';

    private const string FullSha = "2eee1a72cec7d15e725f0433bee374fd7b682c1e";

    /// <summary>Fixed so the emitted version never varies between runs.</summary>
    private static readonly DateTimeOffset BuildTimestamp =
        new(2026, 4, 29, 8, 45, 0, TimeSpan.Zero);

    [TestMethod]
    [DataRow((string?) null, DisplayName = "no commit sha")]
    [DataRow("", DisplayName = "empty commit sha")]
    [DataRow("   ", DisplayName = "whitespace commit sha")]
    [DataRow("abc1234def", DisplayName = "short commit sha")]
    [DataRow(FullSha, DisplayName = "full commit sha")]
    public void InformationalVersion_ContainsNoUnintendedNonAscii(string? commitHash)
    {
        var informationalVersion = GetInformationalVersion(Generate(commitHash));

        AssertNoUnintendedNonAscii(informationalVersion, "AssemblyInformationalVersion");
    }

    [TestMethod]
    public void InformationalVersion_WithCommitSha_IsBuiltWithHeartThenSha()
    {
        var informationalVersion = GetInformationalVersion(Generate(FullSha));

        Assert.AreEqual("Built with \u2665 " + FullSha, informationalVersion);
    }

    /// <summary>
    /// A blank commit hash must take the same branch as a missing one, matching the guard on the
    /// CommitSha/GITHUB_SHA metadata attributes, rather than trailing the heart with whitespace.
    /// </summary>
    [TestMethod]
    [DataRow((string?) null, DisplayName = "null commit sha")]
    [DataRow("", DisplayName = "empty commit sha")]
    [DataRow("   ", DisplayName = "whitespace commit sha")]
    public void InformationalVersion_WithoutUsableCommitSha_IsBuiltWithHeart(string? commitHash)
    {
        var informationalVersion = GetInformationalVersion(Generate(commitHash));

        Assert.AreEqual("Built with " + Heart, informationalVersion);
    }

    /// <summary>
    /// A doubled or re-encoded heart is the shape a mojibake round-trip would take, and it would
    /// still satisfy the allowlist, so the count is pinned separately.
    /// </summary>
    [TestMethod]
    public void InformationalVersion_ContainsExactlyOneHeart()
    {
        var informationalVersion = GetInformationalVersion(Generate(FullSha));

        Assert.AreEqual(1, informationalVersion.Count(character => character == Heart));
    }

    /// <summary>
    /// Every input here is ASCII, so any non-ASCII character in the output other than the branding
    /// heart can only have come from a literal inside the generator itself.
    /// </summary>
    [TestMethod]
    public void GeneratedSource_WithAsciiInputs_ContainsNoUnintendedNonAscii()
    {
        var source = Generate(FullSha, publicVersion: "3.1.0", copyrightHolder: "Brian Bennewitz")
            .NormalizeWhitespace()
            .ToFullString();

        AssertNoUnintendedNonAscii(source, "generated source", allowLineBreaks: true);
    }

    private static CompilationUnitSyntax Generate(
        string? commitHash,
        string? publicVersion = null,
        string? copyrightHolder = null) =>
        AssemblyInfoTemplate.Generate(
            BuildVersion.Generate(BuildTimestamp),
            buildConfiguration: "Release",
            company: "Acme Corp",
            product: "Acme App",
            commitHash: commitHash,
            publicVersion: publicVersion,
            copyrightHolder: copyrightHolder);

    /// <summary>
    /// Pulls the decoded value out of <c>[assembly: AssemblyInformationalVersion("...")]</c>, which
    /// is what reaches assembly metadata, rather than the escaped source form.
    /// </summary>
    private static string GetInformationalVersion(CompilationUnitSyntax compilationUnit)
    {
        var literals = compilationUnit.AttributeLists
            .SelectMany(attributeList => attributeList.Attributes)
            .Where(attribute => IsNamed(attribute, "AssemblyInformationalVersion"))
            .Select(attribute => attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression)
            .OfType<LiteralExpressionSyntax>()
            .ToArray();

        Assert.AreEqual(
            1,
            literals.Length,
            "Expected exactly one [assembly: AssemblyInformationalVersion(\"...\")] with a string literal argument.");

        return (string) literals[0].Token.Value!;
    }

    /// <summary>
    /// The factory strips the "Attribute" suffix when it emits the name; accept either spelling so
    /// this stays correct if that ever changes.
    /// </summary>
    private static bool IsNamed(AttributeSyntax attribute, string name)
    {
        var emitted = attribute.Name.ToString();
        return string.Equals(emitted, name, StringComparison.Ordinal)
            || string.Equals(emitted, name + "Attribute", StringComparison.Ordinal);
    }

    /// <summary>
    /// Allows printable ASCII and the single branding <see cref="Heart" />. Everything else fails,
    /// including control characters, U+FFFD and lone surrogates.
    /// </summary>
    private static void AssertNoUnintendedNonAscii(
        string value,
        string what,
        bool allowLineBreaks = false)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];

            if (character >= FirstPrintableAscii && character <= LastPrintableAscii)
            {
                continue;
            }

            if (character == Heart)
            {
                continue;
            }

            if (allowLineBreaks && (character == '\r' || character == '\n' || character == '\t'))
            {
                continue;
            }

            Assert.Fail(string.Format(
                CultureInfo.InvariantCulture,
                "{0} contains U+{1:X4} at index {2}. Only printable ASCII (U+0020 to U+007E) and the "
                    + "branding heart U+2665 are allowed. Value: {3}",
                what,
                (int) character,
                i,
                value));
        }
    }
}
