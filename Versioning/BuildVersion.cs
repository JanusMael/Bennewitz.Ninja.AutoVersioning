using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

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
    /// Calculates a version from a pre-captured timestamp (e.g. from the BuildTimestamp MSBuild property)
    /// </summary>
    private BuildVersion(DateTimeOffset timestamp)
    {
        Version = CalculateAbsoluteVersion(out var buildDateTimeOffset, timestamp);
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

        if (!TryGetBuildDateTime(
                GetStampedYear(majorVersion, minorVersion, yearOfFirstVersion),
                buildNumber,
                buildRevision,
                out var buildDateTime))
        {
            throw new ArgumentException(
                $"The specified file version info does not contain a valid build number and/or revision: {fileVersionInfo}",
                nameof(fileVersionInfo));
        }

        Version = new Version(majorVersion, minorVersion, buildNumber, buildRevision);
        BuildDateTime = buildDateTime;
    }

    /// <summary>
    /// Recovers the calendar year the version was stamped in.
    /// </summary>
    private static int GetStampedYear(int majorVersion, int minorVersion, int? yearOfFirstVersion) =>
        yearOfFirstVersion.HasValue
            //the 'relative' algorithm was used
            ? minorVersion + yearOfFirstVersion.Value + 1
            //the 'absolute' algorithm was used
            : majorVersion;

    /// <summary>
    /// Decomposes the <c>MMdd</c> build number and <c>HHmm</c> revision back into a build date and
    /// time, returning false when they are not a stamp this type could have produced.
    /// </summary>
    /// <remarks>
    /// Arithmetic, not string parsing, and that is the whole point. Both parts are stored as
    /// integers, so any value below 1000 loses its leading zero: 08:45 is stamped as 0845 and comes
    /// back as 845. The previous implementation round-tripped through ToString() and
    /// DateTime.TryParseExact against the four-character "HHmm" format, so every build between
    /// 01:00 and 09:59 failed to parse; the result was discarded, so those builds silently read back
    /// as midnight. Going through a string also let the machine's culture choose the digits, and an
    /// out-of-range value reached the DateTimeOffset constructor and threw out of a Try method.
    /// </remarks>
    private static bool TryGetBuildDateTime(int year, int buildNumber, int buildRevision, out DateTimeOffset buildDateTime)
    {
        buildDateTime = default;

        if (year < 1 || year > 9999
            || buildNumber < Constants.MinimumValueOfMMdd
            || buildRevision < Constants.MinimumValueOfHHmm)
        {
            return false;
        }

        var month = buildNumber / 100;
        var day = buildNumber % 100;
        var hour = buildRevision / 100;
        var minute = buildRevision % 100;

        if (month < 1 || month > 12
            || hour > 23
            || minute > 59)
        {
            return false;
        }

        if (day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        buildDateTime = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        return true;
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

    /// <inheritdoc cref="BuildVersion(DateTimeOffset)" />
    public static BuildVersion Generate(DateTimeOffset timestamp) => new(timestamp);

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

        // Validate before constructing rather than inside: the strict constructor throws, and a
        // Try method must report an unrecognised version by returning false, not by throwing.
        var isAutoGenerated = TryGetBuildDateTime(
            GetStampedYear(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, yearOfFirstVersion),
            fileVersionInfo.FileBuildPart,
            fileVersionInfo.FilePrivatePart,
            out _);

        if (!isAutoGenerated)
        {
            buildVersion = new BuildVersion(fileVersionInfo);
            return false;
        }

        buildVersion = new BuildVersion(fileVersionInfo, yearOfFirstVersion);
        return true;
    }

    /// <summary>
    /// ISO-ordered date, 24-hour time, and a numeric UTC offset, all culture-invariant.
    /// </summary>
    /// <remarks>
    /// This string is embedded in the generated DirectoryBuildInfo.BuildRelease constant, which the
    /// README points consumers at for health and diagnostic endpoints. The previous
    /// ToShortDateString/ToLongTimeString pair and TimeZoneInfo.Local.StandardName were all
    /// locale-dependent, so the same commit produced a different string per build machine, that
    /// string could be non-ASCII on a non-English system, and a quote character from some locale
    /// would have broken the generated source outright, since the value is interpolated raw.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetDateTimeAndTimeZone(DateTimeOffset dateTimeOffset)
    {
        var date = dateTimeOffset.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var time = dateTimeOffset.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var utcOffset = dateTimeOffset.ToString("zzz", CultureInfo.InvariantCulture);
        return $"{date} {time} (UTC{utcOffset})";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Version CalculateAbsoluteVersion(out DateTimeOffset buildTime, DateTimeOffset? timestamp = null)
    {
        buildTime = timestamp ?? DateTimeOffset.Now;

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

    /// <remarks>
    /// InvariantCulture is load-bearing, not tidiness: these two values become the Build and
    /// Revision parts of the assembly version. Without it the machine's culture chooses the
    /// calendar and the digits, so a non-Gregorian locale would derive a different month and day,
    /// and a locale with non-Latin digits would hand Convert.ToUInt16 something it cannot parse.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static VersionBuildInfo GetBuildInfo(DateTimeOffset buildTime) =>
        new(Convert.ToUInt16(buildTime.ToString(Formats.MonthAndDay, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
            Convert.ToUInt16(buildTime.ToString(Formats.HourAndMinute, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));

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