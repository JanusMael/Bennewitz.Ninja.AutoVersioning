using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

/// <remarks>
/// Ported from the original with one change: the two GetCompositeDeterministicHashCode overloads
/// are omitted, because they depend on a GetDeterministicHashCode() extension that does not exist
/// in this project. For int inputs that overload was equivalent to <see cref="CombineHashCodes(int[])"/>
/// anyway, which is what <c>FuzzyBuildVersionComparer</c> uses.
/// </remarks>
[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
[SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
public static class HashCodeUtility
{
    /// <remarks>
    /// Takes advantage of a struct to convert the into into bytes. The byte fields and int fields will be stored in the same
    /// spot in memory, populating `intval` will populate the byte values as well. (Assumes LittleEndian)
    /// This ends up being faster than BitConverter.GetBytes()
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    private struct IntToByteUnion
    {
        [FieldOffset(0)]
        public Int32 intval;

        [FieldOffset(0)]
        public byte b0;

        [FieldOffset(1)]
        public byte b1;

        [FieldOffset(2)]
        public byte b2;

        [FieldOffset(3)]
        public byte b3;
    }

    /// <remarks>
    /// Optimized version for combining just two arguments.
    /// Roughly takes half the time than calling params version with two arguments.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCompositeHashCode<T1, T2>(T1 item1, T2 item2)
    {
        byte[] bytes = new byte[8];

        var varIntToByteHash = new IntToByteUnion
        {
            intval = item1?.GetHashCode() ?? 0
        };

        bytes[0] = varIntToByteHash.b0;
        bytes[1] = varIntToByteHash.b1;
        bytes[2] = varIntToByteHash.b2;
        bytes[3] = varIntToByteHash.b3;

        varIntToByteHash.intval = item2?.GetHashCode() ?? 0;
        bytes[4] = varIntToByteHash.b0;
        bytes[5] = varIntToByteHash.b1;
        bytes[6] = varIntToByteHash.b2;
        bytes[7] = varIntToByteHash.b3;

        var hash = xxHash32.ComputeHash(bytes, bytes.Length);
        return unchecked((int) hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCompositeHashCode(params object?[] items)
        => CombineHashCodes(items, o => o?.GetHashCode() ?? 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCompositeHashCode<T>(T[] items)
        => CombineHashCodes(items, t => t?.GetHashCode() ?? 0);

    private const int _NumberOfBytesInInteger = 4;

    /// <summary>
    /// Combines the HashCodes of the given list of elements together, using given function to get the hashcode for an
    ///     individual element of the list.
    ///
    ///     Combines the HashCodes together using the xxHash32 algorithm. The algorithm that HashCode.Combine uses and successfully
    ///     passes the SMHasher test suite.
    ///
    /// </summary>
    /// <returns>The combined hashcode of all the elements</returns>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private const byte CombineHashCodesDoc = 0;

    /// <inheritdoc cref="CombineHashCodesDoc"/>
    /// <param name="elements">The list of elements to combine the hash codes of</param>
    /// <param name="getHashCode">The function to get a hash code for an individual item of elements</param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CombineHashCodes<T>(T[] elements, Func<T, int> getHashCode)
    {
        if (elements.Length == 0) return 0;

        var intToByte = new IntToByteUnion();
        var allHashcodeBytes = new byte[elements.Length * _NumberOfBytesInInteger];
        for (int i = 0; i < elements.Length; i++)
        {
            intToByte.intval = elements[i] == null ? 0 : getHashCode(elements[i]);
            int start = i * _NumberOfBytesInInteger;
            allHashcodeBytes[start] = intToByte.b0;
            allHashcodeBytes[start + 1] = intToByte.b1;
            allHashcodeBytes[start + 2] = intToByte.b2;
            allHashcodeBytes[start + 3] = intToByte.b3;
        }

        var hash = xxHash32.ComputeHash(allHashcodeBytes, allHashcodeBytes.Length);
        return unchecked((int) hash);
    }

    /// <inheritdoc cref="CombineHashCodesDoc"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CombineHashCodes(params int[] hashCodes)
    {
        if (hashCodes.Length == 0) return 0;

        var intToByte = new IntToByteUnion();
        var allHashcodeBytes = new byte[hashCodes.Length * _NumberOfBytesInInteger];
        for (int i = 0; i < hashCodes.Length; i++)
        {
            intToByte.intval = hashCodes[i];

            int start = i * _NumberOfBytesInInteger;
            allHashcodeBytes[start] = intToByte.b0;
            allHashcodeBytes[start + 1] = intToByte.b1;
            allHashcodeBytes[start + 2] = intToByte.b2;
            allHashcodeBytes[start + 3] = intToByte.b3;
        }

        var hash = xxHash32.ComputeHash(allHashcodeBytes, allHashcodeBytes.Length);
        return unchecked((int) hash);
    }
}
