using System;
using System.Runtime.CompilerServices;

namespace FactorBasedPermissionsNS.Converters;

internal static class NumericConverter<T>
    where T : unmanaged
{
    public static readonly Func<T, uint> ToUInt32 = CreateConverter();

    private static Func<T, uint> CreateConverter()
    {
        var type = typeof(T);
        var typeOrUnderlying = type.IsEnum ? Enum.GetUnderlyingType(type) : type;

        if (typeOrUnderlying == typeof(byte))
            return static value =>
            {
                var temp = value;
                return Unsafe.As<T, byte>(ref temp);
            };

        if (typeOrUnderlying == typeof(ushort))
            return static value =>
            {
                var temp = value;
                return Unsafe.As<T, ushort>(ref temp);
            };

        if (typeOrUnderlying == typeof(uint))
            return static value =>
            {
                var temp = value;
                return Unsafe.As<T, uint>(ref temp);
            };

        if (typeOrUnderlying == typeof(ulong))
            return static value =>
            {
                var temp = value;
                var result = Unsafe.As<T, ulong>(ref temp);
                return result is > uint.MaxValue
                    ? throw new OverflowException($"Value ({result}) out of range for uint")
                    : (uint)result;
            };

        if (typeOrUnderlying == typeof(sbyte))
            return static value =>
            {
                var temp = value;
                var result = Unsafe.As<T, sbyte>(ref temp);
                return result is >= 0
                    ? (uint)result
                    : throw new OverflowException($"Value ({result}) out of range for uint");
            };

        if (typeOrUnderlying == typeof(short))
            return static value =>
            {
                var temp = value;
                var result = Unsafe.As<T, short>(ref temp);
                return result is >= 0
                    ? (uint)result
                    : throw new OverflowException($"Value ({result}) out of range for uint");
            };

        if (typeOrUnderlying == typeof(int))
            return static value =>
            {
                var temp = value;
                var result = Unsafe.As<T, int>(ref temp);
                return result is >= 0
                    ? (uint)result
                    : throw new OverflowException($"Value ({result}) out of range for uint");
            };

        if (typeOrUnderlying == typeof(long))
            return static value =>
            {
                var temp = value;
                var result = Unsafe.As<T, long>(ref temp);
                return result is >= 0 and <= uint.MaxValue
                    ? (uint)result
                    : throw new OverflowException($"Value ({result}) out of range for uint");
            };

        throw new NotSupportedException($"Unsupported underlying type {typeOrUnderlying.Name}");
    }
}
