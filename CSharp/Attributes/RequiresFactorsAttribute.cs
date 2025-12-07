using FactorBasedPermissionsNS.Attributes.Common;
using System;

namespace FactorBasedPermissionsNS.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class RequiresFactorsAttribute<TFactorId> : ValuesAttribute<TFactorId>
    where TFactorId : unmanaged
{
    public RequiresFactorsAttribute(params TFactorId[] values) : base(values) { }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class RequiresFactorsAttribute : ValuesAttribute<object>
{
    public RequiresFactorsAttribute(params object[] values) : base(values) { }
}
