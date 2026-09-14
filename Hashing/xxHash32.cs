using System;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

/// <summary>
/// XXH32 (xxHash, 32-bit) by Yann Collet. Vendored rather than taken from System.IO.Hashing so
/// this analyzer stays a single self-contained assembly: on netstandard2.0 that package pulls in
/// System.Buffers and System.Memory, none of which are shipped in the analyzers/ folder.
/// </summary>
/// <remarks>
/// xxHash32 is a fixed algorithm, so this produces values identical to any other conforming
/// implementation. Verified against System.IO.Hashing.XxHash32 over 3,005 cases (seeds 0, 1, 42,
/// uint.MaxValue and 2654435761, across lengths 0-600, covering the 16-byte striped loop and both
/// the 4-byte and single-byte tails), plus the published vectors XXH32("")==0x02CC5D05 and
/// XXH32("abc")==0x32D153FF.
/// </remarks>
// ReSharper disable once InconsistentNaming
internal static class xxHash32
{
    private const uint Prime1 = 2654435761U;
    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;

    public static uint ComputeHash(byte[] data, int length, uint seed = 0)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (length < 0 || length > data.Length)
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be within the bounds of the array.");

        unchecked
        {
            int index = 0;
            uint h32;

            if (length >= 16)
            {
                int limit = length - 16;
                uint v1 = seed + Prime1 + Prime2;
                uint v2 = seed + Prime2;
                uint v3 = seed;
                uint v4 = seed - Prime1;

                do
                {
                    v1 = Round(v1, ReadUInt32(data, index)); index += 4;
                    v2 = Round(v2, ReadUInt32(data, index)); index += 4;
                    v3 = Round(v3, ReadUInt32(data, index)); index += 4;
                    v4 = Round(v4, ReadUInt32(data, index)); index += 4;
                } while (index <= limit);

                h32 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            }
            else
            {
                h32 = seed + Prime5;
            }

            h32 += (uint)length;

            while (index + 4 <= length)
            {
                h32 += ReadUInt32(data, index) * Prime3;
                h32 = RotateLeft(h32, 17) * Prime4;
                index += 4;
            }

            while (index < length)
            {
                h32 += (uint)data[index] * Prime5;
                h32 = RotateLeft(h32, 11) * Prime1;
                index++;
            }

            h32 ^= h32 >> 15;
            h32 *= Prime2;
            h32 ^= h32 >> 13;
            h32 *= Prime3;
            h32 ^= h32 >> 16;

            return h32;
        }
    }

    private static uint Round(uint acc, uint input)
    {
        unchecked
        {
            acc += input * Prime2;
            acc = RotateLeft(acc, 13);
            acc *= Prime1;
            return acc;
        }
    }

    private static uint RotateLeft(uint value, int count)
        => unchecked((value << count) | (value >> (32 - count)));

    // Little-endian read, matching the reference implementation on LE platforms.
    private static uint ReadUInt32(byte[] data, int index)
        => unchecked((uint)(data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24)));
}
