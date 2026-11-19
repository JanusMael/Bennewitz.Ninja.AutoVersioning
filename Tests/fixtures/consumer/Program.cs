using System;
using System.IO;
using System.Reflection;
using Bennewitz.Ninja.AutoVersioning;

/// <summary>
/// What a consumer of this package is meant to be able to do, and could not before
/// <c>plans/00002</c>: read its own build's CalVer stamp back off the assembly the build produced.
/// </summary>
/// <remarks>
/// The <c>using</c> above is the whole point of decision 11's namespace: a consumer reads a version
/// number without writing <c>using Bennewitz.Ninja.AutoVersioning.SourceGenerators;</c>.
/// </remarks>
internal static class Program
{
    private static int Main()
    {
        string location = Assembly.GetExecutingAssembly().Location;
        Console.WriteLine($"CONSUMER-ASSEMBLY=[{location}]");

        // BuildVersion arrives from the PACKAGE. Nothing else in this project could supply it: the
        // generator is loaded as an analyzer, which contributes no reference to the compilation.
        if (!BuildVersion.TryGetFromFile(new FileInfo(location), out BuildVersion? buildVersion)
            || buildVersion is null)
        {
            // Not a failure of the package - the type was reachable either way - but of the round
            // trip: this assembly carries no stamp the library recognises.
            Console.WriteLine("CONSUMER-STAMP=[]");
            return 1;
        }

        Console.WriteLine($"CONSUMER-STAMP=[{buildVersion.Version}]");
        Console.WriteLine($"CONSUMER-DESCRIPTION=[{buildVersion}]");

        // FuzzyBuildVersionComparer is public API again as of 2026-11-19, which means a consumer
        // must be able to reach it. Nothing else proves that from outside this repository.
        Console.WriteLine(
            $"CONSUMER-COMPARER=[{FuzzyBuildVersionComparer.Default.Equals(buildVersion, buildVersion)}]");

        return 0;
    }
}
