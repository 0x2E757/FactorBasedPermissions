using FactorBasedPermissionsNS.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FactorBasedPermissionsNS.Helpers;

public static class FactorBasedPermissionsHelpers
{
    private static IEnumerable<T> EmptyEnumerableOrThrow<T>(bool shouldThrow, string message)
    {
        if (shouldThrow)
            throw new Exception(message);

        return Enumerable.Empty<T>();
    }

    public static IEnumerable<TFactorId> GetRequiredFactors<TFactorId>(object target, bool strict = false)
        where TFactorId : unmanaged
    {
        var memberName = target.ToString();
        var memberInfo = target.GetType().GetMember(memberName);

        if (memberInfo is null || memberInfo.Length == 0)
            return EmptyEnumerableOrThrow<TFactorId>(strict, $"Could not extract enum member {memberName}.");

        var attributeTyped = memberInfo[0].GetCustomAttribute<RequiresFactorsAttribute<TFactorId>>();

        if (attributeTyped is not null)
            return attributeTyped.Values;

        var attributeUntyped = memberInfo[0].GetCustomAttribute<RequiresFactorsAttribute>();

        if (attributeUntyped is not null)
            return attributeUntyped.Values.OfType<TFactorId>();

        return EmptyEnumerableOrThrow<TFactorId>(strict, $"Looks like {memberName} is missing {nameof(RequiresFactorsAttribute)}.");
    }

    public static IEnumerable<TPermissionId> GetGrantedPermissions<TPermissionId>(object target, bool strict = false)
        where TPermissionId : unmanaged
    {
        var memberName = target.ToString();
        var memberInfo = target.GetType().GetMember(memberName);

        if (memberInfo is null || memberInfo.Length == 0)
            return EmptyEnumerableOrThrow<TPermissionId>(strict, $"Could not extract enum member {memberName}.");

        var attributeTyped = memberInfo[0].GetCustomAttribute<GrantsPermissionsAttribute<TPermissionId>>();

        if (attributeTyped is not null)
            return attributeTyped.Values;

        var attributeUntyped = memberInfo[0].GetCustomAttribute<GrantsPermissionsAttribute>();

        if (attributeUntyped is not null)
            return attributeUntyped.Values.OfType<TPermissionId>();

        return EmptyEnumerableOrThrow<TPermissionId>(strict, $"Looks like {memberName} is missing {nameof(GrantsPermissionsAttribute)}.");
    }
}
