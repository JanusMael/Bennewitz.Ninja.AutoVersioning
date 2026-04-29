using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

/// <summary>
/// Assembly level attributes interpreted by the C# compiler
///
/// Global attributes appear in the source code after any top level using directives and before any type, module, or
/// namespace declarations.
/// Global attributes can appear in multiple source files, but the files must be compiled in a single compilation pass.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/global
///
/// Assembly attributes are values that provide information about an assembly and these fall into one of
/// the following categories:
///     * Assembly identity attributes
///     * Informational attributes
///     * Assembly manifest attributes
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class AssemblyInfoSyntaxFactory
{
    /// <summary>
    /// all attributes exposed by this factory are assembly-level attributes
    /// </summary>
    private const bool IsAssemblyAttribute = true;

    /// <summary>
    /// Assembly identity attributes
    /// Three attributes (with a strong name, if applicable) determine the identity of an assembly:
    /// name, version, and culture.
    /// These attributes form the full name of the assembly and are required when you reference it in code.
    /// You can set an assembly's version and culture using attributes.
    /// However, the name value is set by the compiler, when the assembly is created.
    /// The assembly name is based on the assembly manifest.
    /// The <see cref="AssemblyFlagsAttribute" /> attribute specifies whether multiple copies of the assembly can coexist.
    /// </summary>
    public static class Identity
    {
        /// <inheritdoc cref="AssemblyVersionAttribute" />
        public static AttributeListSyntax Version(Version version) =>
            AttributeSyntaxFactory.Attribute<AssemblyVersionAttribute>(version.ToString(), IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyCultureAttribute" />
        public static AttributeListSyntax Culture(CultureInfo cultureInfo) =>
            AttributeSyntaxFactory.Attribute<AssemblyCultureAttribute>(cultureInfo.Name, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyFlagsAttribute" />
        public static AttributeListSyntax Flags(AssemblyNameFlags assemblyFlags)
            => AttributeSyntaxFactory.Attribute<AssemblyFlagsAttribute>(
                SyntaxFactory.AttributeArgument(BinaryExpressionBuilder.Build(
                    SyntaxKind.BarToken,
                    assemblyFlags.SplitFlags()
                                          .Select(x => x.MemberAccessExpression())
                                          .OfType<ExpressionSyntax>()
                                          .ToArray()
                    )),
                IsAssemblyAttribute
            );
    }

    /// <summary>
    /// Informational attributes
    ///
    /// You use informational attributes to provide additional company or product information for an assembly.
    /// </summary>
    public static class Informational
    {
        /// <inheritdoc cref="AssemblyProductAttribute" />
        public static AttributeListSyntax Product(string product) =>
            AttributeSyntaxFactory.Attribute<AssemblyProductAttribute>(product, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyTrademarkAttribute" />
        public static AttributeListSyntax Trademark(string trademark) =>
            AttributeSyntaxFactory.Attribute<AssemblyTrademarkAttribute>(trademark, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyInformationalVersionAttribute" />
        public static AttributeListSyntax Version(string version) =>
            AttributeSyntaxFactory.Attribute<AssemblyInformationalVersionAttribute>(version, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyCompanyAttribute" />
        public static AttributeListSyntax Company(string company) =>
            AttributeSyntaxFactory.Attribute<AssemblyCompanyAttribute>(company, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyCopyrightAttribute" />
        public static AttributeListSyntax Copyright(string copyright) =>
            AttributeSyntaxFactory.Attribute<AssemblyCopyrightAttribute>(copyright, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyFileVersionAttribute" />
        public static AttributeListSyntax FileVersion(Version version) =>
            AttributeSyntaxFactory.Attribute<AssemblyFileVersionAttribute>(version.ToString(), IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyMetadataAttribute" />
        public static AttributeListSyntax Metadata(string key, string value) =>
            AttributeSyntaxFactory.Attribute<AssemblyMetadataAttribute>(new[] { key, value }, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyMetadataAttribute" />
        public static AttributeListSyntax Metadata(string key) =>
            AttributeSyntaxFactory.Attribute<AssemblyMetadataAttribute>(new[] { key }, IsAssemblyAttribute);
    }

    /// <summary>
    /// Assembly manifest attributes
    ///
    /// You can use assembly manifest attributes to provide information in the assembly manifest.
    /// The attributes include title, description, default alias, and configuration.
    /// </summary>
    public static class Manifest
    {
        /// <inheritdoc cref="AssemblyTitleAttribute" />
        public static AttributeListSyntax Title(string title) =>
            AttributeSyntaxFactory.Attribute<AssemblyTitleAttribute>(title, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyDescriptionAttribute" />
        public static AttributeListSyntax Description(string description) =>
            AttributeSyntaxFactory.Attribute<AssemblyDescriptionAttribute>(description, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyConfigurationAttribute" />
        public static AttributeListSyntax Configuration(string configuration) =>
            AttributeSyntaxFactory.Attribute<AssemblyConfigurationAttribute>(configuration, IsAssemblyAttribute);

        /// <inheritdoc cref="AssemblyDefaultAliasAttribute" />
        public static AttributeListSyntax DefaultAlias(string defaultAlias) =>
            AttributeSyntaxFactory.Attribute<AssemblyDefaultAliasAttribute>(defaultAlias, IsAssemblyAttribute);
    }

    /// <summary>
    /// Assembly behavior attributes
    ///
    /// You can use assembly behavior attributes to change build-time or run-time behavior of an assembly
    /// </summary>
    public static class Behavioral
    {
        /// <inheritdoc cref="InternalsVisibleToAttribute" />
        public static AttributeListSyntax InternalsVisibleTo(string assemblyName) =>
            AttributeSyntaxFactory.Attribute<InternalsVisibleToAttribute>(assemblyName, IsAssemblyAttribute);
    }
}