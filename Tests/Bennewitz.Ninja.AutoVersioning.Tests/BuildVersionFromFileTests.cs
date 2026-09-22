using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Bennewitz.Ninja.AutoVersioning.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests;

/// <summary>
/// Covers <see cref="BuildVersion.TryGetFromFile(FileInfo, out BuildVersion)" />, which consumers
/// use from their own tests to read back the stamp on a built binary.
/// </summary>
/// <remarks>
/// Each case emits a real assembly carrying a chosen AssemblyFileVersion, so the version is read
/// out of an actual Win32 version resource by FileVersionInfo rather than from a stub.
///
/// The case that matters most is the morning build. Both halves of the stamp are stored as
/// integers, so 08:45 is written as 0845 and read back as 845. The previous implementation fed that
/// through DateTime.TryParseExact against the four-character "HHmm" format, discarded the resulting
/// bool, and so reported midnight for every build between 01:00 and 09:59.
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class BuildVersionFromFileTests
{
    private static string _workDirectory = string.Empty;

    [ClassInitialize]
    public static void CreateWorkDirectory(TestContext context)
    {
        _workDirectory = Path.Combine(Path.GetTempPath(), "autoversioning-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDirectory);
    }

    [ClassCleanup]
    public static void RemoveWorkDirectory()
    {
        try
        {
            if (Directory.Exists(_workDirectory))
            {
                Directory.Delete(_workDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A still-loaded probe assembly is not worth failing a green run over.
        }
    }

    [TestMethod]
    [DataRow("2026.2.429.845", 2026, 4, 29, 8, 45, DisplayName = "08:45 - three-digit revision")]
    [DataRow("2026.2.429.100", 2026, 4, 29, 1, 0, DisplayName = "01:00 - lowest accepted revision")]
    [DataRow("2026.2.429.959", 2026, 4, 29, 9, 59, DisplayName = "09:59 - last three-digit minute")]
    [DataRow("2026.2.429.1000", 2026, 4, 29, 10, 0, DisplayName = "10:00 - first four-digit revision")]
    [DataRow("2026.2.429.1435", 2026, 4, 29, 14, 35, DisplayName = "14:35 - four-digit revision")]
    [DataRow("2026.4.1231.2359", 2026, 12, 31, 23, 59, DisplayName = "23:59 on 31 December")]
    [DataRow("2026.1.101.100", 2026, 1, 1, 1, 0, DisplayName = "01:00 on 1 January")]
    public void TryGetFromFile_RecoversTheStampedDateAndTime(
        string fileVersion,
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        var path = EmitAssemblyWithFileVersion(fileVersion);

        var recognised = BuildVersion.TryGetFromFile(new FileInfo(path), out var buildVersion);

        Assert.IsTrue(recognised, "Expected " + fileVersion + " to be recognised as an auto-generated stamp.");
        Assert.IsNotNull(buildVersion);
        Assert.AreEqual(
            new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero),
            buildVersion.BuildDateTime,
            "Recovered build date/time for " + fileVersion);
    }

    /// <summary>
    /// A version that is not one of ours must come back as false, never as an exception: these
    /// reached the DateTimeOffset constructor and threw straight out of the Try method.
    /// </summary>
    [TestMethod]
    [DataRow("1.0.1332.1435", DisplayName = "month 13")]
    [DataRow("1.0.0031.1435", DisplayName = "month 0")]
    [DataRow("2026.1.230.1200", DisplayName = "30 February")]
    [DataRow("2026.1.132.1200", DisplayName = "32 January")]
    [DataRow("2026.2.429.2499", DisplayName = "hour 24, minute 99")]
    [DataRow("2026.2.429.9999", DisplayName = "revision out of clock range")]
    [DataRow("1.0.0.0", DisplayName = "unstamped default")]
    public void TryGetFromFile_UnrecognisedVersion_ReturnsFalseWithoutThrowing(string fileVersion)
    {
        var path = EmitAssemblyWithFileVersion(fileVersion);

        var recognised = BuildVersion.TryGetFromFile(new FileInfo(path), out var buildVersion);

        Assert.IsFalse(recognised, "Expected " + fileVersion + " to be rejected as a stamp.");
        Assert.IsNotNull(buildVersion, "The version parts should still be reported even when unrecognised.");
        Assert.AreEqual(DateTimeOffset.MinValue, buildVersion.BuildDateTime);
    }

    /// <summary>
    /// The stamp is decomposed arithmetically, so the ambient culture cannot reach it. Asserting the
    /// invariant result keeps this correct whatever a given ICU version decides these cultures mean.
    /// </summary>
    [TestMethod]
    [DataRow("de-DE", DisplayName = "German")]
    [DataRow("th-TH", DisplayName = "Thai")]
    [DataRow("ar-SA", DisplayName = "Saudi Arabic")]
    [DataRow("fa-IR", DisplayName = "Persian")]
    public void TryGetFromFile_IsUnaffectedByAmbientCulture(string cultureName)
    {
        var path = EmitAssemblyWithFileVersion("2026.2.429.845");

        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            var recognised = BuildVersion.TryGetFromFile(new FileInfo(path), out var buildVersion);

            Assert.IsTrue(recognised);
            Assert.IsNotNull(buildVersion);
            Assert.AreEqual(
                new DateTimeOffset(2026, 4, 29, 8, 45, 0, TimeSpan.Zero),
                buildVersion.BuildDateTime);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }

    /// <summary>
    /// The 'relative' algorithm encodes the year as an offset from a first-version year, so the
    /// recovered year comes from the minor part rather than the major one.
    /// </summary>
    [TestMethod]
    public void TryGetFromFile_WithYearOfFirstVersion_UsesTheRelativeAlgorithm()
    {
        // minor 3 + first-version year 2022 + 1 == 2026
        var path = EmitAssemblyWithFileVersion("7.3.429.845");

        var recognised = BuildVersion.TryGetFromFile(new FileInfo(path), yearOfFirstVersion: 2022, out var buildVersion);

        Assert.IsTrue(recognised);
        Assert.IsNotNull(buildVersion);
        Assert.AreEqual(
            new DateTimeOffset(2026, 4, 29, 8, 45, 0, TimeSpan.Zero),
            buildVersion.BuildDateTime);
    }

    /// <summary>
    /// Compiles a throwaway assembly whose only content is an AssemblyFileVersion attribute. The C#
    /// compiler synthesises the Win32 version resource from it, which is what FileVersionInfo reads.
    /// </summary>
    private static string EmitAssemblyWithFileVersion(string fileVersion)
    {
        var assemblyName = "VersionProbe_" + Guid.NewGuid().ToString("N");
        var source = "[assembly: System.Reflection.AssemblyFileVersion(\"" + fileVersion + "\")]";

        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var path = Path.Combine(_workDirectory, assemblyName + ".dll");

        // Emit(path) writes no Win32 version resource, and FileVersionInfo reads nothing else, so
        // the probe has to ask for one explicitly and emit through the stream overload.
        using var win32Resources = compilation.CreateDefaultWin32Resources(
            versionResource: true,
            noManifest: true,
            manifestContents: null,
            iconInIcoFormat: null);

        using (var peStream = File.Create(path))
        {
            var result = compilation.Emit(peStream, win32Resources: win32Resources);

            Assert.IsTrue(
                result.Success,
                "Could not emit the probe assembly: " + string.Join("; ", result.Diagnostics));
        }

        return path;
    }
}
