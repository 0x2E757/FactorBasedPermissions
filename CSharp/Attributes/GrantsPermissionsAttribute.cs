using FactorBasedPermissionsNS.Attributes.Common;
using System;

namespace FactorBasedPermissionsNS.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class GrantsPermissionsAttribute<TPermissionId> : ValuesAttribute<TPermissionId>
    where TPermissionId : unmanaged
{
    public GrantsPermissionsAttribute(params TPermissionId[] values) : base(values) { }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class GrantsPermissionsAttribute : ValuesAttribute<object>
{
    public GrantsPermissionsAttribute(params object[] values) : base(values) { }
}
