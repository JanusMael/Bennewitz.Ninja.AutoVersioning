using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

[SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static partial class ValueTypeExtensions
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static readonly Type TypeOfConvertible = typeof(IConvertible);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T2 ChangeType<T1, T2>(this T1 x, IFormatProvider? provider = null)
        where T1 : struct, IConvertible
        where T2 : struct, IConvertible
    {
        // .NET Standard 2.1 compatible code only here
        Type typeOfT2 = typeof(T2);
        if (typeOfT2.IsEnum)
        {
            return (T2) Enum.Parse(typeOfT2, x.ToString(provider ?? CultureInfo.InvariantCulture));
        }
        return (T2) Convert.ChangeType(x, typeOfT2, provider ?? CultureInfo.InvariantCulture);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConvertible ChangeType(this IConvertible? x, Type toType, IFormatProvider? provider = null)
    {
        if (x == null)
        {
            return null!;
        }

        //if this code looks a little backwards it is because it is NETSTANDARD flavored
        if (toType.IsEnum)
        {
            return (IConvertible) Enum.Parse(toType, x.ToString(provider ?? CultureInfo.InvariantCulture));
        }

        if (TypeOfConvertible.IsAssignableFrom(toType))
        {
            return (IConvertible) Convert.ChangeType(x, toType, provider ?? CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"'{nameof(toType)}' must be derived from '{nameof(IConvertible)}'");
    }
}