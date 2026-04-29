using System;
using System.Runtime.CompilerServices;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

public static class DateTimeOffsetExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetQuarter(this DateTimeOffset dateTimeOffset)
        => (dateTimeOffset.Month + 2) / 3;
}