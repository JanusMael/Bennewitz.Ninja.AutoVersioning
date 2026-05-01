using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

public static class AssemblyInfoTemplate
{
    public static CompilationUnitSyntax Generate(
        BuildVersion buildVersion,
        string buildConfiguration,
        string company,
        string product,
        string? commitHash,
        string? publicVersion,
        string? copyrightHolder = null
    ) =>
        SyntaxFactory.CompilationUnit()
            .WithUsings(
                SyntaxExtensions.UsingNamespaces(
                    SyntaxExtensions.GeneratedCodeHeaderComment(product, buildVersion.Version),
                    typeof(AssemblyVersionAttribute).Namespace ?? string.Empty,
                    typeof(GeneratedCodeAttribute).Namespace ?? string.Empty,
                    typeof(InternalsVisibleToAttribute).Namespace ?? string.Empty
                    )
            )
            .WithAttributeLists(
                GetAttributeList(
                    buildVersion,
                    buildConfiguration,
                    company,
                    product,
                    commitHash,
                    publicVersion,
                    copyrightHolder
                )
            );

    private static SyntaxList<AttributeListSyntax> GetAttributeList(
        BuildVersion buildVersion,
        string buildConfiguration,
        string company,
        string product,
        string? commitHash,
        string? publicVersion,
        string? copyrightHolder = null)
    {
        var list = new List<AttributeListSyntax>(
            new[]
            {
                AssemblyInfoSyntaxFactory.Manifest
                    .Description($"Assembly Version: {buildVersion.Version}"),
                AssemblyInfoSyntaxFactory.Manifest
                    .Configuration(buildConfiguration),
                AssemblyInfoSyntaxFactory.Informational
                    .Company(company),
                AssemblyInfoSyntaxFactory.Informational
                    .Product($"{product} v{buildVersion.Version}"),
                AssemblyInfoSyntaxFactory.Informational
                    .Copyright($"Copyright {buildVersion.BuildDateTime.Year}{(string.IsNullOrWhiteSpace(copyrightHolder) ? "" : $" {copyrightHolder}")} [{buildConfiguration}]"),
                AssemblyInfoSyntaxFactory.Identity
                    .Version(buildVersion.Version),
                AssemblyInfoSyntaxFactory.Informational
                    .FileVersion(buildVersion.Version),
                AssemblyInfoSyntaxFactory.Informational
                    .Version(GetBuildName(commitHash)),
            });

        if (commitHash != null && !string.IsNullOrWhiteSpace(commitHash))
        {
            list.Add(
                AssemblyInfoSyntaxFactory.Informational
                        .Metadata("CommitSha", commitHash)
                );
            list.Add(
                AssemblyInfoSyntaxFactory.Informational
                        .Metadata("GITHUB_SHA", commitHash)
                );
        }

        // 'publicVersion' receives the value of the PublicVersion MSBuild property, which consumers
        // map from their own CI environment variable in Directory.Build.props.
        if (!string.IsNullOrWhiteSpace(publicVersion))
        {
#pragma warning disable CS8604 // Possible null reference argument.
            list.Add(
                AssemblyInfoSyntaxFactory.Informational
                        .Metadata("PublicVersion", publicVersion)
#pragma warning restore CS8604 // Possible null reference argument.
            );
        }

        return list.ToSyntaxList();
    }

    private static string GetBuildName(string? commitHash)
    {
        if (string.IsNullOrEmpty(commitHash))
        {
            return "Built with ♥";
        }

        return $"Commit♥: {commitHash}";
    }
}
