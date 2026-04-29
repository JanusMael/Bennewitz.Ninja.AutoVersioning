using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

/// <summary>
/// Assembly <see cref="Version"/> information that is calculated at build time and the static algorithms to calculate it
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed record BuildVersion
{
    public Version Version { get; }

    public DateTimeOffset BuildDateTime { get; }
    public string BuildTime =>
        GetDateTimeAndTimeZone(BuildDateTime);

    public override string ToString() =>
        $"Version {Version} [built {BuildTime}]";

    /// <summary>
    /// Calculates a version based on the current build year, quarter, date, and time
    /// </summary>
    private BuildVersion()
    {
        Version = CalculateAbsoluteVersion(out var buildDateTimeOffset);
        BuildDateTime = buildDateTimeOffset;
    }

    /// <summary>
    /// Calculates a version based on the specified `major version` with the remaining version parts relative to
    /// the specified `year of the first version` and the current build date and time
    /// </summary>
    private BuildVersion(UInt16 majorVersion, UInt16 yearOfFirstVersion)
    {
        Version = CalculateRelativeVersion(majorVersion, yearOfFirstVersion, out var buildDateTimeOffset);
        BuildDateTime = buildDateTimeOffset;
    }

    /// <summary>
    /// Reads an version from the specified binary file when it is known that the version was auto-generated
    /// </summary>
    private BuildVersion(FileVersionInfo fileVersionInfo, int? yearOfFirstVersion)
    {
        var majorVersion = fileVersionInfo.FileMajorPart;
        var minorVersion = fileVersionInfo.FileMinorPart;
        var buildNumber = fileVersionInfo.FileBuildPart; //aka Build Number
        var buildRevision = fileVersionInfo.FilePrivatePart; //aka Revision

        if (buildNumber < Constants.MinimumValueOfMMdd || buildRevision < Constants.MinimumValueOfHHmm)
        {
            throw new ArgumentException(
                $"The specified file version info does not contain a valid build number and/or revision: {fileVersionInfo}",
                nameof(buildNumber));
        }

        Version = new Version(majorVersion, minorVersion, buildNumber, buildRevision);

        int year;
        if (yearOfFirstVersion.HasValue)
        {
            //the 'relative' algorithm was used
            year = minorVersion + yearOfFirstVersion.Value + 1;
        }
        else
        {
            //the 'absolute' algorithm was used
            year = majorVersion;
        }

        var monthAndDay = buildNumber.ToString();

        var indexOfDay = monthAndDay.Length - 2;

        var day = Convert.ToUInt16(monthAndDay.Substring(indexOfDay, 2));
        var month = Convert.ToUInt16(monthAndDay.Substring(0, indexOfDay));

        DateTime.TryParseExact(buildRevision.ToString(), Formats.HourAndMinute, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime hourAndMinute);

        BuildDateTime = new DateTimeOffset(
            year,
            month,
            day,
            hourAndMinute.Hour,
            hourAndMinute.Minute,
            0,
            TimeSpan.Zero);
    }

    /// <summary>
    /// Reads an version from the specified binary file when the version was NOT auto-generated
    /// </summary>
    private BuildVersion(FileVersionInfo fileVersionInfo)
    {
        var majorVersion = fileVersionInfo.FileMajorPart;
        var minorVersion = fileVersionInfo.FileMinorPart;
        var buildNumber = fileVersionInfo.FileBuildPart; //aka Build Number
        var buildRevision = fileVersionInfo.FilePrivatePart; //aka Revision

        Version = new Version(majorVersion, minorVersion, buildNumber, buildRevision);
        BuildDateTime = DateTimeOffset.MinValue;
    }

    /// <inheritdoc cref="BuildVersion()" />
    public static BuildVersion Generate() => new();

    /// <inheritdoc cref="BuildVersion(UInt16,UInt16)" />
    public static BuildVersion Generate(UInt16 majorVersion, UInt16 yearOfFirstVersion) => new(majorVersion, yearOfFirstVersion);

    public static bool TryGetFromFile(FileInfo fileInfo, out BuildVersion? buildVersion) =>
        TryGetFromFile(fileInfo, null, out buildVersion);

    /// <inheritdoc cref="BuildVersion(FileVersionInfo,Nullable{int})" />
    public static bool TryGetFromFile(FileInfo fileInfo, int? yearOfFirstVersion, out BuildVersion? buildVersion)
    {
        if (fileInfo == null || !fileInfo.Exists)
        {
            throw new FileNotFoundException(fileInfo?.FullName ?? nameof(fileInfo));
        }
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);

        if (fileVersionInfo.FileBuildPart < Constants.MinimumValueOfMMdd || fileVersionInfo.FilePrivatePart < Constants.MinimumValueOfHHmm)
        {
            buildVersion = new BuildVersion(fileVersionInfo);
            return false;
        }

        buildVersion = new BuildVersion(fileVersionInfo, yearOfFirstVersion);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetDateTimeAndTimeZone(DateTimeOffset dateTimeOffset)
    {
        var shortDate = dateTimeOffset.DateTime.ToShortDateString();
        var longTime = dateTimeOffset.DateTime.ToLongTimeString();
        var shortTimeZone = GetShortTimeZone();
        return $"{shortDate} {longTime} ({shortTimeZone})";
    }

    /// <summary>
    /// Calculates a version based on the current build year, quarter, date, and time
    /// </summary>
    /// <param name="buildTime"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Version CalculateAbsoluteVersion(out DateTimeOffset buildTime)
    {
        buildTime = DateTimeOffset.Now;

        //even though Version ctor accepts Int32 parameters,
        //each part must actually be 0-65535 (aka UInt16 or ushort)
        var majorVersion = buildTime.Year;
        var minorVersion = buildTime.GetQuarter();
        var (buildNumber, buildRevision) = GetBuildInfo(buildTime);

        return new Version(majorVersion, minorVersion, buildNumber, buildRevision);
    }

    /// <summary>
    /// Uses the specified `major version` and calculates the remaining version parts relative to the year of the first version
    /// and the current build date and time
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Version CalculateRelativeVersion(UInt16 majorVersion, UInt16 yearOfFirstVersion, out DateTimeOffset buildTime)
    {
        buildTime = DateTimeOffset.Now;

        if (yearOfFirstVersion > buildTime.Year)
        {
            throw new ArgumentException(
                $"'{nameof(yearOfFirstVersion)}' ({yearOfFirstVersion}) must not be in the future (current year: {buildTime.Year})",
                nameof(yearOfFirstVersion));
        }

        //even though Version ctor accepts Int32 parameters,
        //each part must actually be 0-65535 (aka UInt16 or ushort)
        var minorVersion = buildTime.Year - yearOfFirstVersion - 1;
        var (buildNumber, buildRevision) = GetBuildInfo(buildTime);

        return new Version(majorVersion, minorVersion, buildNumber, buildRevision);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static VersionBuildInfo GetBuildInfo(DateTimeOffset buildTime) =>
        new(Convert.ToUInt16(buildTime.ToString(Formats.MonthAndDay)),
            Convert.ToUInt16(buildTime.ToString(Formats.HourAndMinute)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetShortTimeZone()
    {
        var shortTimeZone = new StringBuilder();
        var timeZoneParts = TimeZoneInfo.Local.StandardName.Split(
            new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < timeZoneParts.Length; i++)
        {
            shortTimeZone.Append(timeZoneParts[i][0]);
        }

        return shortTimeZone.ToString();
    }

    private sealed record VersionBuildInfo(UInt16 Number, UInt16 Revision)
    {
        public UInt16 Number { get; } = Number;
        public UInt16 Revision { get; } = Revision;
    }

    private static class Constants
    {
        //100 is the minimum VALID value for the UInt16 of `HHmm` is 0100
        public const UInt16 MinimumValueOfHHmm = 100;
        //101 is the minimum VALID value for the UInt16 of `MMdd` is 0101 (January 1st)
        public const UInt16 MinimumValueOfMMdd = 101;
    }
    private static class Formats
    {
        public const string MonthAndDay = "MMdd";
        public const string HourAndMinute = "HHmm";
    }
}