using System;

namespace FactorBasedPermissionsNS.Serializers;

internal static class NumberSerializer
{
    private const int CacheLimit = 1024;

    private static readonly string[] Precomputed = CreatePrecomputed();

    private static string[] CreatePrecomputed()
    {
        var result = new string[CacheLimit];

        for (uint i = 0; i < CacheLimit; i += 1)
            result[i] = Serialize(i, false);

        return result;
    }

    public static string Serialize(uint value, bool useCache = true)
    {
        if (value == 0)
            return "0";

        if (useCache && value < CacheLimit)
            return Precomputed[value];

        Span<char> buffer = stackalloc char[7];
        var pos = buffer.Length;

        while (value > 0)
        {
            var chunk = value % 32;
            value /= 32;

            var c = chunk < 10
                ? (char)('0' + chunk) // codes 48-57
                : (char)('a' + (chunk - 10)); // codes 97-118

            pos -= 1;
            buffer[pos] = c;
        }

        return new string(buffer[pos..]);
    }

    public static uint Deserialize(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            throw new ArgumentException("Input cannot be empty", nameof(value));

        ulong result = 0;

        for (var n = 0; n < value.Length; n += 1)
            result = result * 32 + DecodeDigit(value[n]);

        if (result > uint.MaxValue)
            throw new OverflowException($"Deserialized value {result} does not fit into a 32-bit unsigned integer");

        return (uint)result;
    }

    public static uint Deserialize(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length == 0)
            throw new ArgumentException("Input cannot be empty", nameof(value));

        return Deserialize(value.AsSpan());
    }

    private static uint DecodeDigit(char value)
    {
        if (value is >= '0' and <= '9')
            return (uint)(value - '0');

        if (value is >= 'a' and <= 'v')
            return (uint)(value - 'a' + 10);

        if (value is >= 'A' and <= 'V')
            return (uint)(value - 'A' + 10);

        throw new FormatException($"Invalid Base32 character: '{value}'");
    }
}
