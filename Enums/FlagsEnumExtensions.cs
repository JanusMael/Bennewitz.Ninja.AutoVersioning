using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

/// <summary>
/// Convert back and forth between <see cref="Enum"/>s annotated with <see cref="FlagsAttribute"/> and <see cref="Enum"/> arrays.
/// </summary>
/// <remarks>
/// This is useful for classes that serialize/deserialize flags:
///     * makes the serialized version human-readable
///     * better support for interop of serialization format with other languages
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class FlagsEnumExtensions
{
    private static readonly EnumTypeInfoCache EnumTypeInfoCache = new(
        enumType =>
        {
            var hasFlagsAttribute = enumType.HasFlagsAttribute();
            if (!hasFlagsAttribute)
            {
                throw new ArgumentException(
                    $"'{nameof(enumType)}' must be an '{nameof(Enum)}' annotated with '{nameof(FlagsAttribute)}'"
                );
            }

            return true;
        });

    /// <summary>
    /// Converts an array of individual flag <see cref="Enum"/> values to the bitwise-or of those values
    /// </summary>
    /// <param name="flags">
    /// an array containing individual values from an <see cref="Enum"/> annotated with <see cref="FlagsAttribute"/>
    /// </param>
    /// <typeparam name="T">
    /// an <see cref="Enum"/> annotated with <see cref="FlagsAttribute"/>
    /// </typeparam>
    /// <returns>
    /// an <see cref="Enum"/> representing input flag values bitwise-or-combined
    /// </returns>
    /// <remarks>
    /// This is useful for deserialization and interop scenarios
    /// </remarks>
    public static T CombineFlags<T>(this T[] flags)
        where T : struct, Enum
    {
        var typeOfT = typeof(T);
        var enumTypeInfo = EnumTypeInfoCache.GetOrAdd(typeOfT);
        long result = 0;

        for (int i = 0; i < flags.Length; i++)
        {
            var value = Convert.ToInt64(flags[i]);
            if (!EnumTypeInfo.DefaultUnderlyingValue.Equals(value))
            {
                result |= value;
            }
        }
        return (T) Convert.ChangeType(result, enumTypeInfo.UnderlyingType, null);
    }

    /// <summary>
    /// Converts an <see cref="Enum"/> of bitwise-or-combined flags to an array of individual flag values
    /// </summary>
    /// <param name="flags">
    /// an <see cref="Enum"/> representing flag values combined via bitwise-or
    /// </param>
    /// <typeparam name="T">
    /// an <see cref="Enum"/> annotated with <see cref="FlagsAttribute"/>
    /// </typeparam>
    /// <returns>
    /// an array containing individual values from the input bitwise-or-combined <see cref="Enum"/> value
    /// </returns>
    /// <remarks>
    /// This is useful for serialization and interop scenarios
    /// </remarks>
    public static T[] SplitFlags<T>(this T flags)
        where T : struct, Enum
    {
        var typeOfT = typeof(T);
        var enumTypeInfo = EnumTypeInfoCache.GetOrAdd(typeOfT);

        if (!enumTypeInfo.HasFlagsAttribute)
        {
            throw new ArgumentException($"'{typeOfT.Name}' must be annotated with '{nameof(FlagsAttribute)}'");
        }

        List<T> flagList = new List<T>();
        long flagsValue = Convert.ToInt64(flags);
        foreach (var enumValue in enumTypeInfo.Values)
        {
            T flag = (T) enumValue.Value;
            long flagValue = Convert.ToInt64(flag);
            if (!EnumTypeInfo.DefaultUnderlyingValue.Equals(flagValue) &&
                (flagsValue & flagValue) == flagValue)
            {
                flagList.Add(flag);
            }
        }
        return flagList.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlagsAttribute(this Type enumType) =>
        enumType.GetCustomAttributes(typeof(FlagsAttribute), false).FirstOrDefault() is not null;
}