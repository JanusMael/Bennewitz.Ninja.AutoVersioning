using System;
using System.Runtime.CompilerServices;

namespace Bennewitz.Ninja.AutoVersioning;

/// <remarks>
/// ⛔ <c>internal</c> deliberately, per <c>plans/00002</c> decision 8. As a public extension method
/// on <see cref="DateTimeOffset"/> it would appear on every <see cref="DateTimeOffset"/> in every
/// consuming project the moment this assembly ships a <c>lib/</c>.
/// </remarks>
internal static class DateTimeOffsetExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetQuarter(this DateTimeOffset dateTimeOffset)
        => (dateTimeOffset.Month + 2) / 3;
}