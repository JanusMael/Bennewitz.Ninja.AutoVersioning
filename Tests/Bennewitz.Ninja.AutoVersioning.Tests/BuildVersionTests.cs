using System;
using System.Globalization;
using System.Threading;
using Bennewitz.Ninja.AutoVersioning.SourceGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests;

/// <summary>
/// Pins <see cref="BuildVersion" /> to the build machine's clock and nothing else.
/// </summary>
/// <remarks>
/// Both halves of this type used to read the ambient culture. The version parts came from
/// ToString("MMdd") and ToString("HHmm"), so a non-Gregorian calendar would have derived a
/// different month and day and a locale with non-Latin digits would have handed Convert.ToUInt16
/// something it could not parse; the build-time string came from ToShortDateString,
/// ToLongTimeString and TimeZoneInfo.Local.StandardName, so the same commit produced a different,
/// possibly non-ASCII string per machine. That string is embedded in DirectoryBuildInfo.BuildRelease
/// and surfaces on consumers' diagnostic endpoints.
///
/// These tests assert the invariant result under each culture. They are therefore correct whatever
/// a given ICU version decides those cultures mean: the only way to fail is for the code to consult
/// the ambient culture at all.
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class BuildVersionTests
{
    /// <summary>29 April 2026, 08:45:00, five hours behind UTC. Q2, so the minor version is 2.</summary>
    private static readonly DateTimeOffset BuildTimestamp =
        new(2026, 4, 29, 8, 45, 0, TimeSpan.FromHours(-5));

    private const string ExpectedVersion = "2026.2.429.845";
    private const string ExpectedBuildTime = "2026-04-29 08:45:00 (UTC-05:00)";

    [TestMethod]
    [DataRow("de-DE", DisplayName = "German: '.' date separator, ',' decimal")]
    [DataRow("th-TH", DisplayName = "Thai: Buddhist calendar, year off by 543")]
    [DataRow("ar-SA", DisplayName = "Saudi Arabic: Umm al-Qura calendar")]
    [DataRow("fa-IR", DisplayName = "Persian: Solar Hijri calendar")]
    [DataRow("ja-JP", DisplayName = "Japanese: non-Latin era and day-period text")]
    public void Version_IsUnaffectedByAmbientCulture(string cultureName)
    {
        WithCulture(cultureName, () =>
        {
            var buildVersion = BuildVersion.Generate(BuildTimestamp);

            Assert.AreEqual(ExpectedVersion, buildVersion.Version.ToString());
        });
    }

    [TestMethod]
    [DataRow("de-DE", DisplayName = "German")]
    [DataRow("th-TH", DisplayName = "Thai")]
    [DataRow("ar-SA", DisplayName = "Saudi Arabic")]
    [DataRow("fa-IR", DisplayName = "Persian")]
    [DataRow("ja-JP", DisplayName = "Japanese")]
    public void BuildTime_IsUnaffectedByAmbientCulture(string cultureName)
    {
        WithCulture(cultureName, () =>
        {
            var buildVersion = BuildVersion.Generate(BuildTimestamp);

            Assert.AreEqual(ExpectedBuildTime, buildVersion.BuildTime);
        });
    }

    /// <summary>
    /// The whole rendered string is what lands in DirectoryBuildInfo.BuildRelease, interpolated raw
    /// into generated C#, so it must be printable ASCII and must contain no quote or backslash that
    /// would terminate or escape the surrounding literal.
    /// </summary>
    [TestMethod]
    [DataRow("de-DE", DisplayName = "German")]
    [DataRow("th-TH", DisplayName = "Thai")]
    [DataRow("ar-SA", DisplayName = "Saudi Arabic")]
    [DataRow("fa-IR", DisplayName = "Persian")]
    [DataRow("ja-JP", DisplayName = "Japanese")]
    public void ToString_IsPrintableAsciiAndSafeToEmbed(string cultureName)
    {
        WithCulture(cultureName, () =>
        {
            var rendered = BuildVersion.Generate(BuildTimestamp).ToString();

            for (var i = 0; i < rendered.Length; i++)
            {
                var character = rendered[i];

                Assert.IsTrue(
                    character >= ' ' && character <= '~',
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "BuildRelease text contains U+{0:X4} at index {1} under {2}. Value: {3}",
                        (int) character,
                        i,
                        cultureName,
                        rendered));
            }

            Assert.IsFalse(rendered.Contains("\""), "A quote would terminate the generated string literal.");
            Assert.IsFalse(rendered.Contains("\\"), "A backslash would start an escape in the generated string literal.");
        });
    }

    /// <summary>
    /// Sets both the culture and the UI culture for the duration of <paramref name="act" /> and
    /// restores them afterwards, so a failing assertion cannot leak a culture into later tests.
    /// </summary>
    private static void WithCulture(string cultureName, Action act)
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            act();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }
}
