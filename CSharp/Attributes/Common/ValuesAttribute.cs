using System;

namespace FactorBasedPermissionsNS.Attributes.Common;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ValuesAttribute<T> : Attribute
    where T : notnull
{
    public T[] Values { get; }

    public ValuesAttribute(params T[] values)
    {
        Values = values;
    }
}
