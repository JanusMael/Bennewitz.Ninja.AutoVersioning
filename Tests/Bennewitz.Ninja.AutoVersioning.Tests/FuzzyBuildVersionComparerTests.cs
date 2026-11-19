using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bennewitz.Ninja.AutoVersioning.Tests;

/// <summary>
/// <see cref="FuzzyBuildVersionComparer"/>, which is a <b>supported public API</b> of
/// <c>Bennewitz.Ninja.AutoVersioning.Abstractions</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ It had <b>no test at all</b> until 2026-11-19. Its rule row in <c>Versioning/AGENTS.md</c> read
/// "Guarded by: Nothing automated", which was tolerable while it was unused internal code and is not
/// now that it is published: a consumer can depend on every behaviour asserted here.
/// </para>
/// <para>
/// ⚠ The two subtleties below were previously only <i>described</i> in the type's own remarks.
/// A comment is not a guard, so each is asserted: the <c>HHmm</c> revision is not a linear scale,
/// and the equality relation is deliberately <b>not transitive</b>.
/// </para>
/// </remarks>
[TestClass]
public sealed class FuzzyBuildVersionComparerTests
{
    private static BuildVersion At(int hour, int minute) =>
        BuildVersion.Generate(new DateTimeOffset(2026, 11, 19, hour, minute, 0, TimeSpan.Zero));

    [TestMethod]
    public void Default_IsThirtyMinutes()
    {
        Assert.AreEqual(TimeSpan.FromMinutes(30), FuzzyBuildVersionComparer.Default.RevisionTolerance);
    }

    [TestMethod]
    public void WithinTolerance_IsEqual()
    {
        Assert.IsTrue(FuzzyBuildVersionComparer.Default.Equals(At(10, 0), At(10, 29)));
    }

    [TestMethod]
    public void BeyondTolerance_IsNotEqual()
    {
        Assert.IsFalse(FuzzyBuildVersionComparer.Default.Equals(At(10, 0), At(10, 31)));
    }

    /// <summary>
    /// ⛔ The property the whole type turns on: <c>Revision</c> is <c>HHmm</c>-encoded, so 09:59 and
    /// 10:00 are one minute apart although the integers 959 and 1000 differ by 41.
    /// </summary>
    /// <remarks>
    /// Comparing the raw integers with a 30-minute window would call these two <b>unequal</b>, and
    /// would call 10:00 and 10:30 equal only by coincidence of the encoding. This is the test that
    /// fails if anyone "simplifies" the decode away.
    /// </remarks>
    [TestMethod]
    public void TheRevisionIsDecodedFromHHmm_NotComparedAsAnInteger()
    {
        BuildVersion justBeforeTen = At(9, 59);
        BuildVersion ten = At(10, 0);

        Assert.AreEqual(959, justBeforeTen.Version.Revision, "The fixture is wrong, not the comparer.");
        Assert.AreEqual(1000, ten.Version.Revision, "The fixture is wrong, not the comparer.");

        FuzzyBuildVersionComparer oneMinute = new(TimeSpan.FromMinutes(1));

        Assert.IsTrue(
            oneMinute.Equals(justBeforeTen, ten),
            "09:59 and 10:00 are one minute apart. Comparing the HHmm integers 959 and 1000 would "
            + "make them 41 apart and this would fail.");
    }

    /// <summary>
    /// The non-transitivity the type's remarks warn about, asserted rather than asserted-in-prose.
    /// </summary>
    [TestMethod]
    public void TheRelationIsDeliberatelyNotTransitive()
    {
        FuzzyBuildVersionComparer threeMinutes = new(TimeSpan.FromMinutes(3));

        Assert.IsTrue(threeMinutes.Equals(At(10, 0), At(10, 3)));
        Assert.IsTrue(threeMinutes.Equals(At(10, 3), At(10, 6)));
        Assert.IsFalse(
            threeMinutes.Equals(At(10, 0), At(10, 6)),
            "A sliding window is not a bucket. If this ever passes, the remarks warning against "
            + "transitive chains — deduplication by sorting, for one — are no longer true.");
    }

    /// <summary>
    /// ⛔ The <see cref="IEqualityComparer{T}"/> contract: two instances this calls equal must hash
    /// alike, which is why <c>Revision</c> is left out of the hash entirely.
    /// </summary>
    [TestMethod]
    public void EqualInstancesHashAlike()
    {
        BuildVersion ten = At(10, 0);
        BuildVersion later = At(10, 29);

        Assert.IsTrue(FuzzyBuildVersionComparer.Default.Equals(ten, later), "The fixture is wrong.");
        Assert.AreEqual(
            FuzzyBuildVersionComparer.Default.GetHashCode(ten),
            FuzzyBuildVersionComparer.Default.GetHashCode(later),
            "Equal instances that hash differently break every Dictionary and HashSet using this "
            + "comparer. Revision is excluded from the hash for exactly this reason.");
    }

    [TestMethod]
    public void ADifferentDay_IsNeverEqual()
    {
        BuildVersion today = BuildVersion.Generate(new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero));
        BuildVersion tomorrow = BuildVersion.Generate(new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero));

        Assert.IsFalse(
            FuzzyBuildVersionComparer.Default.Equals(today, tomorrow),
            "Build (MMdd) must match exactly, which is what lets the revision comparison assume one "
            + "calendar day.");
    }

    [TestMethod]
    public void NullsFollowTheComparerContract()
    {
        FuzzyBuildVersionComparer comparer = FuzzyBuildVersionComparer.Default;

        Assert.IsTrue(comparer.Equals(null, null));
        Assert.IsFalse(comparer.Equals(At(10, 0), null));
        Assert.IsFalse(comparer.Equals(null, At(10, 0)));
        Assert.Throws<ArgumentNullException>(() => comparer.GetHashCode(null!));
    }

    [TestMethod]
    public void ANegativeToleranceIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FuzzyBuildVersionComparer(TimeSpan.FromMinutes(-1)));
    }

    /// <summary>A zero tolerance still matches an identical revision, rather than nothing at all.</summary>
    [TestMethod]
    public void AZeroToleranceMatchesOnlyTheSameMinute()
    {
        FuzzyBuildVersionComparer exact = new(TimeSpan.Zero);

        Assert.IsTrue(exact.Equals(At(10, 0), At(10, 0)));
        Assert.IsFalse(exact.Equals(At(10, 0), At(10, 1)));
    }
}
