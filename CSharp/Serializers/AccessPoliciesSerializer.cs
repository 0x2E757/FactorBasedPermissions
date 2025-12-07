using FactorBasedPermissionsNS.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace FactorBasedPermissionsNS.Serializers;

internal static class AccessPoliciesSerializer
{
    private const char SatisfiedFactorsPrefix = '!'; // code 33
    private const char PermissionsPrefix = '#'; // code 35

    private const char ItemsDelim = ','; // code 44
    private const char GroupDelim = '&'; // code 38
    private const char RequiredFactorsDelim = '+'; // code 43

    public static string Serialize<TFactorId, TPermissionId>(FactorBasedPermissions<TFactorId, TPermissionId> factorBasedPermissions)
        where TFactorId : unmanaged
        where TPermissionId : unmanaged
    {
        if (factorBasedPermissions is null)
            throw new ArgumentNullException(nameof(factorBasedPermissions));

        var estimatedMaxSize = factorBasedPermissions.FactorsLookup.Count * 4 + factorBasedPermissions.PermissionsLookup.Count * 8;
        var sb = new StringBuilder(estimatedMaxSize);

        SerializeSatisfiedFactors();
        SerializePermissions();

        return sb.ToString();

        void SerializeSatisfiedFactors()
        {
            foreach (var kvp in factorBasedPermissions.FactorsLookup)
                if (kvp.Value)
                    AppendValue(sb, kvp.Key, ItemsDelim);

            if (sb.Length > 0)
                sb[0] = SatisfiedFactorsPrefix;
        }

        void SerializePermissions()
        {
            var groups = new Dictionary<string, List<TPermissionId>>();

            foreach (var kvp in factorBasedPermissions.PermissionsLookup)
            {
                var factorsKey = BuildFactorsKey(kvp.Value);

                if (!groups.TryGetValue(factorsKey, out var permList))
                {
                    permList = new List<TPermissionId>();
                    groups[factorsKey] = permList;
                }

                permList.Add(kvp.Key);
            }

            var groupDelim = PermissionsPrefix;

            foreach (var group in groups)
            {
                for (var n = 0; n < group.Value.Count; n += 1)
                    AppendValue(sb, group.Value[n], n > 0 ? ItemsDelim : groupDelim);

                if (group.Key.Length > 0)
                    AppendValue(sb, group.Key, RequiredFactorsDelim);

                groupDelim = GroupDelim;
            }
        }
    }

    private static string BuildFactorsKey<TFactorId>(IReadOnlyList<TFactorId> factors)
        where TFactorId : unmanaged
    {
        if (factors is null || factors.Count == 0)
            return string.Empty;

        var nums = new uint[factors.Count];

        for (var n = 0; n < factors.Count; n += 1)
            nums[n] = NumericConverter<TFactorId>.ToUInt32(factors[n]);

        Array.Sort(nums);

        var sb = new StringBuilder(factors.Count * 4);

        for (var n = 0; n < nums.Length; n += 1)
            AppendValue(sb, nums[n], ItemsDelim, n > 0);

        return sb.ToString();
    }

    private static void AppendValue(StringBuilder sb, string value, char prefix, bool appendPrefix = true)
    {
        if (appendPrefix)
            sb.Append(prefix);

        sb.Append(value);
    }

    private static void AppendValue<T>(StringBuilder sb, T value, char prefix, bool appendPrefix = true)
        where T : unmanaged
    {
        var valueNumeric = NumericConverter<T>.ToUInt32(value);
        var valueSerialized = NumberSerializer.Serialize(valueNumeric);

        AppendValue(sb, valueSerialized, prefix, appendPrefix);
    }

    public static FactorBasedPermissions<TFactorId, TPermissionId> Deserialize<TFactorId, TPermissionId>(string accessPolicies, Func<uint, TFactorId>? factorConverter, Func<uint, TPermissionId>? permissionConverter)
        where TFactorId : unmanaged
        where TPermissionId : unmanaged
    {
        if (accessPolicies is null)
            throw new ArgumentNullException(nameof(accessPolicies));

        factorConverter ??= BoxingConverter<TFactorId>;
        permissionConverter ??= BoxingConverter<TPermissionId>;

        var index = 0;
        var span = accessPolicies.AsSpan();
        var satisfiedFactors = DeserializeSatisfiedFactors(span);
        var permissions = DeserializePermissions(span);

        return new FactorBasedPermissions<TFactorId, TPermissionId>(satisfiedFactors, permissions);

        List<TFactorId> DeserializeSatisfiedFactors(ReadOnlySpan<char> span)
        {
            var result = new List<TFactorId>();

            if (span.Length > index && span[index] == SatisfiedFactorsPrefix)
            {
                var satisfiedFactorsRaw = ParseGroup(span, index + 1, out var endIndex);

                for (var n = 0; n < satisfiedFactorsRaw.Count; n += 1)
                    result.Add(factorConverter(satisfiedFactorsRaw[n]));

                index = endIndex;
            }

            return result;
        }

        Dictionary<TPermissionId, List<TFactorId>> DeserializePermissions(ReadOnlySpan<char> span)
        {
            var result = new Dictionary<TPermissionId, List<TFactorId>>();

            if (span.Length > index && span[index] == PermissionsPrefix)
                while (span.Length > index + 1)
                {
                    var permissionsRaw = ParseGroup(span, index + 1, out var end);
                    var requiredFactorsRaw = end < span.Length && span[end] == RequiredFactorsDelim
                        ? ParseGroup(span, end + 1, out end)
                        : new List<uint>();

                    var requiredFactors = new List<TFactorId>(requiredFactorsRaw.Count);

                    for (var n = 0; n < requiredFactorsRaw.Count; n += 1)
                        requiredFactors.Add(factorConverter(requiredFactorsRaw[n]));

                    for (var n = 0; n < permissionsRaw.Count; n += 1)
                        result.Add(permissionConverter(permissionsRaw[n]), requiredFactors);

                    index = end;
                }

            return result;
        }
    }

    private static List<uint> ParseGroup(ReadOnlySpan<char> span, int startIndex, out int endIndex)
    {
        var result = new List<uint>(16);
        var valueStartIndex = startIndex;

        for (endIndex = startIndex; endIndex < span.Length; endIndex += 1)
        {
            var symbol = span[endIndex];

            if (symbol <= ItemsDelim)
            {
                if (symbol != ItemsDelim)
                    break;

                var str = span[valueStartIndex..endIndex];
                var num = NumberSerializer.Deserialize(str);

                result.Add(num);
                valueStartIndex = endIndex + 1;
            }
        }

        if (valueStartIndex < endIndex)
        {
            var str = span[valueStartIndex..endIndex];
            var num = NumberSerializer.Deserialize(str);

            result.Add(num);
        }

        return result;
    }

    private static T BoxingConverter<T>(uint value)
        where T : unmanaged
    {
        var type = typeof(T);

        if (type.IsEnum)
            return (T)Enum.ToObject(type, value);

        return (T)Convert.ChangeType(value, type);
    }
}
