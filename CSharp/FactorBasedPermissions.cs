using FactorBasedPermissionsNS.Extensions;
using FactorBasedPermissionsNS.Helpers;
using FactorBasedPermissionsNS.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactorBasedPermissionsNS;

public class FactorBasedPermissions<TFactorId, TPermissionId>
    where TFactorId : unmanaged
    where TPermissionId : unmanaged
{
    public Dictionary<TFactorId, bool> FactorsLookup { get; private set; }
    public Dictionary<TPermissionId, List<TFactorId>> PermissionsLookup { get; private set; }
    public Dictionary<TPermissionId, bool> HasPermissionLookup { get; private set; } = new();

    public FactorBasedPermissions()
    {
        FactorsLookup = new();
        PermissionsLookup = new();
    }

    public FactorBasedPermissions(IEnumerable<TFactorId> satisfiedFactors, Dictionary<TPermissionId, List<TFactorId>> permissionsLookup)
    {
        FactorsLookup = new();
        PermissionsLookup = permissionsLookup;

        foreach (var satisfiedFactor in satisfiedFactors)
            FactorsLookup.TryAdd(satisfiedFactor, true);

        foreach (var permissionLookup in permissionsLookup)
            foreach (var requiredFactor in permissionLookup.Value)
                FactorsLookup.TryAdd(requiredFactor, false);
    }

    public FactorBasedPermissions(IEnumerable<TFactorId> satisfiedFactors, IEnumerable<TPermissionId> permissions)
        : this()
    {
        foreach (var satisfiedFactor in satisfiedFactors)
            FactorsLookup.TryAdd(satisfiedFactor, true);

        foreach (var permission in permissions)
        {
            var requiredFactors = FactorBasedPermissionsHelpers
                .GetRequiredFactors<TFactorId>(permission)
                .ToList();

            foreach (var requiredFactor in requiredFactors)
                FactorsLookup.TryAdd(requiredFactor, false);

            PermissionsLookup.TryAdd(permission, requiredFactors);
        }
    }

    public FactorBasedPermissions(IEnumerable<TFactorId> satisfiedFactors, Enum role)
        : this(satisfiedFactors, role.GetGrantedPermissions<TPermissionId>()) { }

    public bool FactorsSatisfied(IEnumerable<TFactorId> factors)
    {
        foreach (var factor in factors)
            if (FactorsLookup.GetValueOrDefault(factor) == false)
                return false;

        return true;
    }

    public bool HasPermission(TPermissionId id, out bool satisfied)
    {
        if (HasPermissionLookup.TryGetValue(id, out satisfied))
            return true;

        if (PermissionsLookup.TryGetValue(id, out var requiredFactors))
        {
            satisfied = FactorsSatisfied(requiredFactors);
            HasPermissionLookup[id] = satisfied;
            return true;
        }

        satisfied = false;
        return false;
    }

    public bool HasPermission(TPermissionId id, bool? satisfiedFilter = true)
    {
        if (HasPermission(id, out var satisfied))
            return satisfiedFilter == satisfied || satisfiedFilter is null;

        return false;
    }

    public bool Same(FactorBasedPermissions<TFactorId, TPermissionId> other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || FactorsLookup.Count != other.FactorsLookup.Count || PermissionsLookup.Count != other.PermissionsLookup.Count)
            return false;

        foreach (var factor in FactorsLookup)
            if (!other.FactorsLookup.ContainsKey(factor.Key) || other.FactorsLookup[factor.Key] != factor.Value)
                return false;

        foreach (var permissionLookup in PermissionsLookup)
        {
            if (!other.PermissionsLookup.ContainsKey(permissionLookup.Key) || permissionLookup.Value.Count != other.PermissionsLookup[permissionLookup.Key].Count)
                return false;

            foreach (var factor in permissionLookup.Value)
                if (!other.PermissionsLookup[permissionLookup.Key].Contains(factor))
                    return false;
        }

        return true;
    }

    public string Serialize()
    {
        return AccessPoliciesSerializer.Serialize(this);
    }

    public static FactorBasedPermissions<TFactorId, TPermissionId> Deserialize(string value, Func<uint, TFactorId>? factorConverter = null, Func<uint, TPermissionId>? permissionConverter = null)
    {
        return AccessPoliciesSerializer.Deserialize(value, factorConverter, permissionConverter);
    }
}
