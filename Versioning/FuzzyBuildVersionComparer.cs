using System;
using System.Collections.Generic;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

/// <summary>
/// Compares <see cref="BuildVersion"/> instances for equality, treating two versions as equal
/// when <see cref="Version.Major"/>, <see cref="Version.Minor"/>, and <see cref="Version.Build"/>
/// match exactly and <see cref="Version.Revision"/> (encoded as <c>HHmm</c>) falls within a
/// configurable time tolerance of each other.
/// </summary>
/// <remarks>
/// Because the tolerance defines a sliding window rather than a fixed bucket, this equality
/// relation is not guaranteed to be transitive: with a 3-minute tolerance, 10:00 and 10:03 are
/// equal, and 10:03 and 10:06 are equal, but 10:00 and 10:06 are not. Avoid relying on
/// transitive chains (e.g. deduplication via sorting) when using this comparer.
/// </remarks>
public sealed class FuzzyBuildVersionComparer : IEqualityComparer<BuildVersion>
{
    /// <summary>
    /// Shared comparer using a 30-minute tolerance. Construct the type directly for any other window.
    /// </summary>
    public static readonly FuzzyBuildVersionComparer Default = new(TimeSpan.FromMinutes(30));

    public FuzzyBuildVersionComparer(TimeSpan revisionTolerance)
    {
        if (revisionTolerance < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(revisionTolerance), revisionTolerance, "Tolerance must not be negative.");

        RevisionTolerance = revisionTolerance;
    }

    public TimeSpan RevisionTolerance { get; }

    public bool Equals(BuildVersion? x, BuildVersion? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        var xVersion = x.Version;
        var yVersion = y.Version;

        return xVersion.Major == yVersion.Major
            && xVersion.Minor == yVersion.Minor
            && xVersion.Build == yVersion.Build
            && RevisionsWithinTolerance(xVersion.Revision, yVersion.Revision);
    }

    public int GetHashCode(BuildVersion obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        // Revision is deliberately excluded: two instances that Equals() treats as equal (because
        // their Revision values fall within tolerance of each other, not because they're identical)
        // must still produce the same hash code, per the IEqualityComparer<T> contract.
        var version = obj.Version;
        return HashCodeUtility.CombineHashCodes(version.Major, version.Minor, version.Build);
    }

    private bool RevisionsWithinTolerance(int revisionX, int revisionY)
    {
        if (revisionX == revisionY) return true;

        var minutesX = RevisionToMinutesSinceMidnight(revisionX);
        var minutesY = RevisionToMinutesSinceMidnight(revisionY);

        return Math.Abs(minutesX - minutesY) <= RevisionTolerance.TotalMinutes;
    }

    // Revision is HHmm-encoded (e.g. 1435 == 14:35), not a linear scale — decode back to
    // minutes-since-midnight before comparing. Safe to assume same calendar day here since
    // Build (MMdd) is already required to match exactly in Equals() above.
    private static int RevisionToMinutesSinceMidnight(int revision)
    {
        var hour = revision / 100;
        var minute = revision % 100;
        return hour * 60 + minute;
    }
}
