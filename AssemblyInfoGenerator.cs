using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class AssemblyInfoGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor GeneratorNotEnabledDescriptor = new(
        id: "BAUTOVERSIONING00",
        title: "Assembly Info Generator not enabled",
        messageFormat: "Assembly info generator is installed but not enabled — see BAUTOVERSIONING04 for setup instructions.",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GeneratorSetupInstructionsDescriptor = new(
        id: "BAUTOVERSIONING04",
        title: "Assembly Info Generator setup instructions",
        // First line is self-contained — build output panels only show line 1 of a multi-line message.
        // The full <PropertyGroup> snippet that follows is visible in IDE detail/hover panes.
        messageFormat: "Add to Directory.Build.props: <GenerateAutoVersionedAssemblyInfo>true</GenerateAutoVersionedAssemblyInfo>"
                     + ", <AssemblyCompany>YourCompany</AssemblyCompany>"
                     + ", <AssemblyProduct>YourProduct</AssemblyProduct>\n"
                     + "  <PropertyGroup>\n"
                     + "    <GenerateAutoVersionedAssemblyInfo>true</GenerateAutoVersionedAssemblyInfo>\n"
                     + "    <AssemblyCompany>YourCompany</AssemblyCompany>\n"
                     + "    <AssemblyProduct>YourProduct</AssemblyProduct>\n"
                     + "    <CopyrightHolder>YourName</CopyrightHolder> <!-- optional -->\n"
                     + "    <PublicVersion Condition=\"'$(PublicVersion)' == ''\">$(YOUR_CI_VERSION_VAR)</PublicVersion> <!-- optional -->\n"
                     + "    <CommitSha Condition=\"'$(CommitSha)' == ''\">$(GITHUB_SHA)</CommitSha> <!-- optional -->\n"
                     + "    <IsContinuousIntegration>$(GITHUB_ACTIONS)</IsContinuousIntegration> <!-- optional -->\n"
                     + "  </PropertyGroup>",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenerationFailedDescriptor = new(
        id: "BAUTOVERSIONING01",
        title: "Assembly Info Generation Failed",
        messageFormat: "Generator threw an exception: {0}",
        category: "SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AssemblyCompanyNotSetDescriptor = new(
        id: "BAUTOVERSIONING02",
        title: "AssemblyCompany not configured",
        messageFormat: "AssemblyCompany is not set — add <AssemblyCompany>YourCompany</AssemblyCompany> to Directory.Build.props ([assembly: AssemblyCompany] will be empty without it)",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AssemblyProductNotSetDescriptor = new(
        id: "BAUTOVERSIONING03",
        title: "AssemblyProduct not configured",
        messageFormat: "AssemblyProduct is not set — add <AssemblyProduct>YourProduct</AssemblyProduct> to Directory.Build.props ([assembly: AssemblyProduct] will be empty without it)",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Read MSBuild properties safely (replaces reading Environment variables and .env files)
        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) => new
            {
                // Properties must be exposed in csproj via <CompilerVisibleProperty Include="PropertyName" />
                // They are accessed via the "build_property." prefix
                GenerateAutoVersionedAssemblyInfo = provider.GlobalOptions.TryGetValue("build_property.GenerateAutoVersionedAssemblyInfo", out var gen) && gen.Equals("true", StringComparison.OrdinalIgnoreCase),
                IsContinuousIntegration = provider.GlobalOptions.TryGetValue("build_property.IsContinuousIntegration", out var isCI) && isCI.Equals("true", StringComparison.OrdinalIgnoreCase),
                CommitSha = provider.GlobalOptions.TryGetValue("build_property.CommitSha", out var sha) ? sha : null,
                PublicVersion = provider.GlobalOptions.TryGetValue("build_property.PublicVersion", out var publicVersion) ? publicVersion : null,
                Configuration = provider.GlobalOptions.TryGetValue("build_property.Configuration", out var config) ? config : "Release",
                AssemblyCompany = provider.GlobalOptions.TryGetValue("build_property.AssemblyCompany", out var assemblyCompany) ? assemblyCompany : null,
                AssemblyProduct = provider.GlobalOptions.TryGetValue("build_property.AssemblyProduct", out var assemblyProduct) ? assemblyProduct : null,
                CopyrightHolder = provider.GlobalOptions.TryGetValue("build_property.CopyrightHolder", out var copyrightHolder) ? copyrightHolder : null
            });

        // 2. Register the source output
        context.RegisterSourceOutput(optionsProvider, (spc, options) =>
        {
            if (!options.GenerateAutoVersionedAssemblyInfo)
            {
                spc.ReportDiagnostic(Diagnostic.Create(GeneratorNotEnabledDescriptor, Location.None));
                spc.ReportDiagnostic(Diagnostic.Create(GeneratorSetupInstructionsDescriptor, Location.None));
                return;
            }

            // Warn in the IDE when required properties are missing
            if (string.IsNullOrWhiteSpace(options.AssemblyCompany))
                spc.ReportDiagnostic(Diagnostic.Create(AssemblyCompanyNotSetDescriptor, Location.None));
            if (string.IsNullOrWhiteSpace(options.AssemblyProduct))
                spc.ReportDiagnostic(Diagnostic.Create(AssemblyProductNotSetDescriptor, Location.None));

            try
            {
                // Note: To optimize further in a real IDE, you might only regenerate the BuildVersion
                // if it's an actual CI build, otherwise fallback to a static version to prevent
                // continuous infinite background recompilation loops in Rider/VS.
                var buildVersion = BuildVersion.Generate();

                // Pass the safely extracted MSBuild properties to the template
                var sourceTree = AssemblyInfoTemplate.Generate(
                    buildVersion,
                    options.Configuration,
                    options.AssemblyCompany ?? string.Empty,
                    options.AssemblyProduct ?? string.Empty,
                    options.CommitSha,
                    options.PublicVersion,
                    options.CopyrightHolder ?? options.AssemblyCompany
                ).NormalizeWhitespace();

                spc.AddSource("AutoVersionedAssemblyInfo.g.cs", SourceText.From(sourceTree.ToFullString(), Encoding.UTF8));

                // Expose build-time version as a constant for runtime use (e.g. passing to error trackers,
                // log enrichment, health endpoints) without reading environment variables at runtime.
                var buildInfoSource =
$@"
// <auto-generated/>
namespace Bennewitz.Ninja.AutoVersioning
{{
   /// <summary>
   /// Build-time constants generated by the source generator.
   /// </summary>
   public static class DirectoryBuildInfo
   {{
       /// <summary>The build version string calculated at compile time.</summary>
       public const string BuildRelease = ""{buildVersion}"";
   }}
}}";
                spc.AddSource("DirectoryBuildInfo.g.cs", SourceText.From(buildInfoSource, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(GenerationFailedDescriptor, Location.None, ex.Message));
            }
        });
    }
}
