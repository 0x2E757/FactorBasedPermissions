using FactorBasedPermissionsNS.Helpers;
using System;
using System.Collections.Generic;

namespace FactorBasedPermissionsNS.Extensions;

public static class EnumAttributeExtensions
{
    public static IEnumerable<TFactorId> GetRequiredFactors<TFactorId>(this Enum target, bool strict = false)
        where TFactorId : unmanaged
    {
        return FactorBasedPermissionsHelpers.GetRequiredFactors<TFactorId>(target, strict);
    }

    public static IEnumerable<TPermissionId> GetGrantedPermissions<TPermissionId>(this Enum target, bool strict = false)
        where TPermissionId : unmanaged
    {
        return FactorBasedPermissionsHelpers.GetGrantedPermissions<TPermissionId>(target, strict);
    }
}
