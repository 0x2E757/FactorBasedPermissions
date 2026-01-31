# Factor-Based Permissions (C#)

A flexible, attribute-driven authorization library that models permissions as combinations of **factors**. Designed for **compact serialization** to fit inside JWT tokens without bloating their size.

## Why Factor-Based Permissions?

Traditional role-based access control (RBAC) often leads to role explosion. Factor-based permissions offer a more granular approach:

- **Factors** represent conditions (e.g., "email verified", "subscription active", "IP whitelisted")
- **Permissions** are granted when **all required factors** are satisfied
- **Roles** can grant sets of permissions declaratively via attributes

### JWT-Optimized Serialization

The primary design goal is **minimal payload size** for embedding in JWT tokens:

| Technique | Benefit |
|-----------|---------|
| Base32 encoding | 5 bits per character vs 3.3 bits in decimal |
| Permission grouping | Permissions sharing same factors are merged |
| Short delimiters | Single-character separators (`!`, `#`, `&`, `+`, `,`) |
| No key names | Positional format, no JSON overhead |

**Example:** A policy with 10 permissions and 5 factors typically serializes to **30-50 characters** — small enough to fit in a JWT claim without concern.

## Installation

```bash
dotnet add package FactorBasedPermissions
```

## Quick Start

### 1. Define Your Factors and Permissions

```csharp
public enum Factor
{
    EmailVerified = 1,
    PhoneVerified = 2,
    SubscriptionActive = 3,
    TwoFactorEnabled = 4,
    AdminApproved = 5
}

public enum Permission
{
    [RequiresFactors(Factor.EmailVerified)]
    ViewDashboard = 1,

    [RequiresFactors(Factor.EmailVerified, Factor.SubscriptionActive)]
    DownloadReports = 2,

    [RequiresFactors(Factor.EmailVerified, Factor.TwoFactorEnabled)]
    ManageApiKeys = 3,

    [RequiresFactors(Factor.AdminApproved)]
    AccessAdminPanel = 4
}
```

### 2. Define Roles (Optional)

```csharp
public enum Role
{
    [GrantsPermissions(
        Permission.ViewDashboard
    )]
    Guest = 1,

    [GrantsPermissions(
        Permission.ViewDashboard,
        Permission.DownloadReports,
        Permission.ManageApiKeys
    )]
    User = 2,

    [GrantsPermissions(
        Permission.ViewDashboard,
        Permission.DownloadReports,
        Permission.ManageApiKeys,
        Permission.AccessAdminPanel
    )]
    Admin = 3
}
```

### 3. Create and Serialize Permissions

```csharp
// Factors satisfied by the current user (determined by your business logic)
var satisfiedFactors = new[] { Factor.EmailVerified, Factor.SubscriptionActive };

// Option A: From a role
var permissions = new FactorBasedPermissions<Factor, Permission>(satisfiedFactors, Role.User);

// Option B: From explicit permission list
var permissions = new FactorBasedPermissions<Factor, Permission>(
    satisfiedFactors,
    new[] { Permission.ViewDashboard, Permission.DownloadReports }
);

// Serialize for JWT
string serialized = permissions.Serialize();
// Example output: "!1,3#1+1&2+1,3"
```

### 4. Store in JWT

```csharp
var claims = new[]
{
    new Claim("sub", userId),
    new Claim("ap", permissions.Serialize())  // "ap" = access policies
};

var token = new JwtSecurityToken(
    issuer: "your-app",
    audience: "your-app",
    claims: claims,
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: credentials
);
```

### 5. Deserialize and Check (Server-Side)

```csharp
// Extract from JWT claim
string serialized = User.FindFirst("ap")?.Value;

// Deserialize
var permissions = FactorBasedPermissions<Factor, Permission>.Deserialize(serialized);

// Check permissions
if (permissions.HasPermission(Permission.DownloadReports))
{
    // Access granted
}

// Check with detailed result
if (permissions.HasPermission(Permission.ManageApiKeys, out bool satisfied))
{
    // Permission exists in policy
    if (satisfied)
    {
        // All required factors are met
    }
    else
    {
        // Permission exists but factors not satisfied
    }
}
else
{
    // Permission not in policy at all
}
```

## Serialization Format

The format is designed for minimal size while remaining debuggable:

```
[!<satisfied_factors>][#<permission_group_1>&<permission_group_2>&...]
```

Both sections are **optional**:
- If no factors are satisfied, the `!...` section is omitted
- If no permissions are defined, the `#...` section is omitted
- An empty policy serializes to an empty string `""`

### Structure

| Symbol | Meaning |
|--------|---------|
| `!` | Start of satisfied factors list (optional) |
| `#` | Start of permissions block (optional) |
| `,` | Item separator within a list |
| `&` | Permission group separator |
| `+` | Separator between permissions and their required factors |

### Number Encoding

All numeric IDs are encoded in **Base32** using characters `0-9` and `a-v`:

| Decimal | Base32 |
|---------|--------|
| 0-9 | 0-9 |
| 10-31 | a-v |
| 32 | 10 |
| 100 | 34 |
| 1000 | v8 |

### Example Breakdown

```
!1,3#1+1&2+1,3
```

- `!1,3` — Satisfied factors: 1 (EmailVerified), 3 (SubscriptionActive)
- `#1+1` — Permission 1 (ViewDashboard) requires factor 1
- `&2+1,3` — Permission 2 (DownloadReports) requires factors 1 and 3

### Grouping Optimization

Permissions with identical required factors are grouped together:

```csharp
// These three permissions all require factor 1:
Permission.A  // requires Factor.X
Permission.B  // requires Factor.X  
Permission.C  // requires Factor.X

// Serialized as: #1,2,3+1
// Instead of:    #1+1&2+1&3+1  (longer)
```

## API Reference

### FactorBasedPermissions&lt;TFactorId, TPermissionId&gt;

#### Constructors

```csharp
// Empty instance
new FactorBasedPermissions<Factor, Permission>()

// From satisfied factors and permission lookup dictionary
new FactorBasedPermissions<Factor, Permission>(
    IEnumerable<Factor> satisfiedFactors,
    Dictionary<Permission, List<Factor>> permissionsLookup
)

// From satisfied factors and permissions (reads RequiresFactors attributes)
new FactorBasedPermissions<Factor, Permission>(
    IEnumerable<Factor> satisfiedFactors,
    IEnumerable<Permission> permissions
)

// From satisfied factors and role (reads GrantsPermissions attribute)
new FactorBasedPermissions<Factor, Permission>(
    IEnumerable<Factor> satisfiedFactors,
    Enum role
)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `FactorsLookup` | `Dictionary<TFactorId, bool>` | All factors with their satisfaction status |
| `PermissionsLookup` | `Dictionary<TPermissionId, List<TFactorId>>` | Permission to required factors mapping |
| `HasPermissionLookup` | `Dictionary<TPermissionId, bool>` | Cached permission check results |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `HasPermission(id, out satisfied)` | `bool` | Returns true if permission exists; `satisfied` indicates if factors are met |
| `HasPermission(id, satisfiedFilter)` | `bool` | Check with filter: `true` = only satisfied, `false` = only unsatisfied, `null` = any |
| `FactorsSatisfied(factors)` | `bool` | Check if all given factors are satisfied |
| `Same(other)` | `bool` | Deep equality comparison |
| `Serialize()` | `string` | Convert to compact string format |

#### Static Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Deserialize(value, factorConverter?, permissionConverter?)` | `FactorBasedPermissions<...>` | Parse from serialized string |

### Attributes

#### RequiresFactorsAttribute

Apply to permission enum members to declare required factors:

```csharp
// Generic version (type-safe)
[RequiresFactors<Factor>(Factor.A, Factor.B)]
SomePermission = 1

// Non-generic version (for dynamic scenarios)
[RequiresFactors(Factor.A, Factor.B)]
SomePermission = 1
```

#### GrantsPermissionsAttribute

Apply to role enum members to declare granted permissions:

```csharp
// Generic version (type-safe)
[GrantsPermissions<Permission>(Permission.X, Permission.Y)]
SomeRole = 1

// Non-generic version
[GrantsPermissions(Permission.X, Permission.Y)]
SomeRole = 1
```

## Advanced Usage

### Custom Type Converters

You can provide custom converters when deserializing. This is especially useful for **enum types** to avoid boxing overhead and improve performance:

```csharp
// Explicit converters are MUCH faster than implicit boxing conversion
var permissions = FactorBasedPermissions<AuthFactor, Permission>.Deserialize(
    serialized,
    factorConverter: id => (AuthFactor)id,
    permissionConverter: id => (Permission)id
);
```

For non-enum numeric types:

```csharp
var permissions = FactorBasedPermissions<int, int>.Deserialize(
    serialized,
    factorConverter: id => (int)id,
    permissionConverter: id => (int)id
);
```

### Comparing Permission Sets

```csharp
var permissions1 = new FactorBasedPermissions<Factor, Permission>(factors1, Role.User);
var permissions2 = new FactorBasedPermissions<Factor, Permission>(factors2, Role.User);

if (permissions1.Same(permissions2))
{
    // Identical factors and permissions
}
```

### Extension Methods

```csharp
// Get required factors for any enum member
var factors = Permission.DownloadReports.GetRequiredFactors<Factor>();

// Get granted permissions for any role
var permissions = Role.Admin.GetGrantedPermissions<Permission>();
```

## Best Practices

### 1. Consider Your Use of Value 0

```csharp
// Option A: Start at 1 to distinguish from default/uninitialized
public enum Permission
{
    ViewDashboard = 1,
    DownloadReports = 2,
    ...
}

// Option B: Use 0 intentionally when it has semantic meaning
public enum AuthFactor
{
    AnyMethod = 0,      // Valid: "any authentication method"
    Password = 10,
    Google = 11,
    ...
}
```

Starting at 1 helps catch uninitialized values, but 0 is perfectly valid when it represents a meaningful concept.

### 2. Group Related Factors

Keep factor IDs grouped logically for easier debugging of serialized strings:

```csharp
public enum Factor
{
    // Verification factors: 1-10
    EmailVerified = 1,
    PhoneVerified = 2,
    
    // Subscription factors: 11-20
    SubscriptionActive = 11,
    SubscriptionPremium = 12,
    
    // Security factors: 21-30
    TwoFactorEnabled = 21,
    ...
}
```

### 3. Use a DI Service for Permission Checks

Create a scoped service to lazily deserialize and cache permissions per request:

```csharp
public class UserPermissions : IUserPermissions
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private FactorBasedPermissions<Factor, Permission>? _permissions;
    private bool _initialized;

    public UserPermissions(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public FactorBasedPermissions<Factor, Permission>? Current
    {
        get
        {
            if (!_initialized)
            {
                _permissions = Deserialize();
                _initialized = true;
            }
            return _permissions;
        }
    }

    public bool HasPermission(Permission permission, bool? satisfiedFilter = true)
    {
        return Current?.HasPermission(permission, satisfiedFilter) ?? false;
    }

    private FactorBasedPermissions<Factor, Permission>? Deserialize()
    {
        var serialized = _httpContextAccessor.HttpContext?.User.FindFirst("ap")?.Value;

        if (serialized is null)
            return null;

        return FactorBasedPermissions<Factor, Permission>.Deserialize(serialized, ToFactor, ToPermission);

        static Factor ToFactor(uint x) => (Factor)x;
        static Permission ToPermission(uint x) => (Permission)x;
    }
}

public interface IUserPermissions
{
    FactorBasedPermissions<Factor, Permission> Current { get; }
    bool HasPermission(Permission permission, bool? satisfiedFilter = true);
}

// Registration
services.AddScoped<IUserPermissions, UserPermissions>();
```

## Size Comparison

| Approach | Example Size | Notes |
|----------|--------------|-------|
| JSON array of permission names | ~200 bytes | `["ViewDashboard","DownloadReports",...]` |
| JSON object with factors | ~300 bytes | Full permission-to-factors mapping |
| This library | **~40 bytes** | `!1,3#1+1&2+1,3&3+1,4&4+5` |

For JWTs that are sent with every HTTP request, this 5-8x size reduction matters.

## Requirements

- .NET Standard 2.1+
- C# 11+

## License

MIT
