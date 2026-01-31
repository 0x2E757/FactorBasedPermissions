# Factor-Based Permissions

A flexible authorization system that models permissions as combinations of **factors**. Optimized for **compact serialization** to fit inside JWT tokens.

## The Concept

Instead of simple role-based checks, permissions are granted when **all required factors** are satisfied:

```
Permission: DownloadReports
  └── Requires: [EmailVerified, SubscriptionActive]
        └── Granted only if BOTH factors are satisfied
```

**Factors** represent conditions like:
- Authentication method (password, social login, MFA)
- Verification status (email verified, identity confirmed)
- Subscription state (active, premium tier)
- Security requirements (2FA enabled, trusted device)

## Why Factor-Based?

| Traditional RBAC | Factor-Based |
|------------------|--------------|
| Role explosion as requirements grow | Compose permissions from reusable factors |
| Hard to express "verified + 2FA required" | Natural: `[RequiresFactors(Factor.EmailVerified, Factor.MfaEnabled)]` |
| Permission logic scattered in code | Declarative attributes on enum members |

## JWT-Optimized Serialization

The primary design goal is **minimal payload size** for embedding in JWT tokens:

```
!1,3#1+1&2+1,3&3+4
 │   │
 │   └── Permissions with their required factors
 └────── Satisfied factors
```

| Technique | Benefit |
|-----------|---------|
| Base32 encoding | 5 bits per character (vs 3.3 in decimal) |
| Permission grouping | Permissions with same factors are merged |
| Single-char delimiters | `!`, `#`, `&`, `+`, `,` |
| No key names | Positional format, zero JSON overhead |

**Result:** A policy with 10 permissions typically serializes to **30-50 characters**.

## Packages

| Platform | Package | Source | Purpose |
|----------|---------|--------|---------|
| .NET | [NuGet](https://www.nuget.org/packages/FactorBasedPermissions) | [CSharp](./CSharp/) | Full implementation: create, serialize, deserialize, check |
| TypeScript | [npm](https://www.npmjs.com/package/factor-based-permissions) | [TypeScript](./TypeScript/) | Client-side: parse and check permissions from JWT |

## Quick Example

**Server (C#):** Define and serialize permissions

```csharp
public enum Factor
{
    EmailVerified = 1,
    SubscriptionActive = 2,
    MfaEnabled = 3
}

public enum Permission
{
    [RequiresFactors(Factor.EmailVerified)]
    ViewDashboard = 1,

    [RequiresFactors(Factor.EmailVerified, Factor.SubscriptionActive)]
    DownloadReports = 2
}

// Create and serialize for JWT
var permissions = new FactorBasedPermissions<Factor, Permission>(
    satisfiedFactors: [Factor.EmailVerified, Factor.SubscriptionActive],
    permissions: [Permission.ViewDashboard, Permission.DownloadReports]
);

string serialized = permissions.Serialize(); // "!1,2#1+1&2+1,2"
```

**Client (TypeScript):** Parse and check

```typescript
import { FactorBasedPermissions } from "factor-based-permissions";

const permissions = new FactorBasedPermissions<Factor, Permission>(jwt.ap);

if (permissions.checkPermission(Permission.DownloadReports)) {
  showDownloadButton();
}

// Show user what's missing
const missing = permissions.getMissingFactors(Permission.DownloadReports);
```

## Documentation

- [C# Documentation](./CSharp/README.md) — Full API reference, serialization format, best practices
- [TypeScript Documentation](./TypeScript/README.md) — Client-side usage, React/Vue patterns

## License

MIT
