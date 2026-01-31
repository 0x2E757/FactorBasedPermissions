# Factor-Based Permissions (TypeScript)

A lightweight TypeScript library for **parsing and checking** factor-based permissions on the client side. Designed to work with permissions serialized by the [C# Factor-Based Permissions](../CSharp/README.md) library and embedded in JWT tokens.

## Why This Library?

When using factor-based permissions with JWTs:

1. **Server** (C#) creates and serializes permissions into a compact string
2. **JWT** carries this string in a claim (typically 30-50 characters)
3. **Client** (TypeScript) parses and checks permissions for UI decisions

This library handles step 3 — fast, dependency-free permission checking in the browser.

## Installation

```bash
npm install factor-based-permissions
```

## Quick Start

### 1. Extract Serialized Permissions from JWT

```typescript
import { FactorBasedPermissions } from "factor-based-permissions";

// Assuming you've decoded your JWT and extracted the "ap" claim
const serialized = decodedJwt.ap; // e.g., "!1,3#1+1&2+1,3"

const permissions = new FactorBasedPermissions(serialized);
```

### 2. Check Permissions

```typescript
// Define your permission enum (matching server-side values)
enum Permission {
  ViewDashboard = 1,
  DownloadReports = 2,
  ManageApiKeys = 3,
  AccessAdminPanel = 4,
}

// Check if permission is granted
const canDownload = permissions.checkPermission(Permission.DownloadReports);

if (canDownload === true) {
  // Permission granted — all required factors satisfied
  showDownloadButton();
} else if (canDownload === false) {
  // Permission exists but factors not satisfied
  showDownloadButtonDisabled();
} else {
  // canDownload === null — permission not in policy
  hideDownloadButton();
}
```

### 3. Show Missing Requirements to User

```typescript
enum Factor {
  EmailVerified = 1,
  PhoneVerified = 2,
  SubscriptionActive = 3,
  TwoFactorEnabled = 4,
}

const missing = permissions.getMissingFactors(Permission.ManageApiKeys);

if (missing.length > 0) {
  // Show user what they need to do
  const messages = missing.map((factor) => {
    switch (factor) {
      case Factor.EmailVerified:
        return "Verify your email";
      case Factor.TwoFactorEnabled:
        return "Enable two-factor authentication";
      default:
        return "Complete verification";
    }
  });

  showRequirementsDialog(messages);
}
```

## API Reference

### Constructor

```typescript
new FactorBasedPermissions<TFactor extends number, TPermission extends number>(
  serialized?: string | null | undefined
)
```

Creates a new instance. If `serialized` is provided, parses the permission data immediately.

```typescript
// With data
const permissions = new FactorBasedPermissions("!1,3#1+1&2+1,3");

// Empty instance (all checks return null)
const empty = new FactorBasedPermissions();
const alsoEmpty = new FactorBasedPermissions(null);
```

### checkPermission

```typescript
checkPermission(permission: TPermission): boolean | null
```

Check if a permission is granted.

| Return Value | Meaning |
|--------------|---------|
| `true` | Permission exists and all required factors are satisfied |
| `false` | Permission exists but some required factors are missing |
| `null` | Permission is not defined in the policy |

```typescript
const result = permissions.checkPermission(Permission.DownloadReports);

// Use in conditionals
if (result === true) {
  // Granted
}

// Truthy check (true only)
if (result) {
  // Granted
}

// Falsy check catches both false and null
if (!result) {
  // Not granted (either missing factors or not in policy)
}
```

### getSatisfiedFactors

```typescript
getSatisfiedFactors(permission?: TPermission): TFactor[]
```

Get satisfied factors, optionally filtered by a specific permission.

```typescript
// All satisfied factors
const allSatisfied = permissions.getSatisfiedFactors();
// [1, 3] (Factor.EmailVerified, Factor.SubscriptionActive)

// Satisfied factors relevant to a specific permission
const satisfiedForDownload = permissions.getSatisfiedFactors(Permission.DownloadReports);
// [1, 3] if permission requires factors 1 and 3, and both are satisfied
```

### getMissingFactors

```typescript
getMissingFactors(permission: TPermission): TFactor[]
```

Get factors that are required for a permission but not satisfied.

```typescript
const missing = permissions.getMissingFactors(Permission.ManageApiKeys);
// [4] if ManageApiKeys requires factors 1 and 4, but only 1 is satisfied
```

### serialized

```typescript
get serialized(): string
```

Returns the original serialized string (useful for passing to other contexts).

```typescript
const original = permissions.serialized;
// "!1,3#1+1&2+1,3"
```

## Serialization Format

The format is identical to the C# library:

```
[!<satisfied_factors>][#<permission_groups>]
```

Both sections are **optional**:
- If no factors are satisfied, the `!...` section is omitted
- If no permissions are defined, the `#...` section is omitted
- An empty policy serializes to an empty string `""`

### Example

```
!1,3#1+1&2+1,3&3+1,4
```

Breakdown:
- `!1,3` — Factors 1 and 3 are satisfied
- `#1+1` — Permission 1 requires factor 1
- `&2+1,3` — Permission 2 requires factors 1 and 3
- `&3+1,4` — Permission 3 requires factors 1 and 4

### Number Encoding

Numbers are encoded in **Base32** (characters `0-9` and `a-v`):

```typescript
// The library handles this automatically via parseInt(value, 32)
parseInt("v8", 32); // 1000
parseInt("10", 32); // 32
```

## Usage Patterns

### React Hook

```typescript
import { useMemo } from "react";
import { FactorBasedPermissions } from "factor-based-permissions";

function usePermissions(serialized: string | null) {
  return useMemo(
    () => new FactorBasedPermissions(serialized),
    [serialized]
  );
}

// In component
function Dashboard() {
  const { accessPolicies } = useAuth(); // Get from JWT/context
  const permissions = usePermissions(accessPolicies);

  return (
    <div>
      {permissions.checkPermission(Permission.ViewDashboard) && (
        <DashboardContent />
      )}
      {permissions.checkPermission(Permission.DownloadReports) && (
        <DownloadButton />
      )}
    </div>
  );
}
```

### Permission Guard Component

```typescript
interface PermissionGuardProps {
  permission: Permission;
  permissions: FactorBasedPermissions<Factor, Permission>;
  children: React.ReactNode;
  fallback?: React.ReactNode;
  onMissingFactors?: (factors: Factor[]) => void;
}

function PermissionGuard({
  permission,
  permissions,
  children,
  fallback = null,
  onMissingFactors,
}: PermissionGuardProps) {
  const result = permissions.checkPermission(permission);

  if (result === true) {
    return <>{children}</>;
  }

  if (result === false && onMissingFactors) {
    const missing = permissions.getMissingFactors(permission);
    onMissingFactors(missing);
  }

  return <>{fallback}</>;
}

// Usage
<PermissionGuard
  permission={Permission.ManageApiKeys}
  permissions={permissions}
  fallback={<UpgradePrompt />}
  onMissingFactors={(factors) => trackMissingFactors(factors)}
>
  <ApiKeysManager />
</PermissionGuard>
```

### Vue Composable

```typescript
import { computed, type Ref } from "vue";
import { FactorBasedPermissions } from "factor-based-permissions";

export function usePermissions(serialized: Ref<string | null>) {
  const permissions = computed(
    () => new FactorBasedPermissions(serialized.value)
  );

  const can = (permission: Permission) =>
    permissions.value.checkPermission(permission) === true;

  const cannot = (permission: Permission) =>
    permissions.value.checkPermission(permission) !== true;

  const missingFor = (permission: Permission) =>
    permissions.value.getMissingFactors(permission);

  return { permissions, can, cannot, missingFor };
}
```

### Utility Functions

```typescript
// Check multiple permissions at once
function hasAllPermissions(
  permissions: FactorBasedPermissions<Factor, Permission>,
  required: Permission[]
): boolean {
  return required.every((p) => permissions.checkPermission(p) === true);
}

// Check if any permission is granted
function hasAnyPermission(
  permissions: FactorBasedPermissions<Factor, Permission>,
  required: Permission[]
): boolean {
  return required.some((p) => permissions.checkPermission(p) === true);
}

// Get all granted permissions from a list
function getGrantedPermissions(
  permissions: FactorBasedPermissions<Factor, Permission>,
  toCheck: Permission[]
): Permission[] {
  return toCheck.filter((p) => permissions.checkPermission(p) === true);
}
```

## Type Safety

The library uses generics for type-safe factor and permission IDs:

```typescript
// Define your enums
enum Factor {
  EmailVerified = 1,
  SubscriptionActive = 3,
}

enum Permission {
  ViewDashboard = 1,
  DownloadReports = 2,
}

// Type-safe usage
const permissions = new FactorBasedPermissions<Factor, Permission>(serialized);

// TypeScript knows these return Factor[]
const satisfied: Factor[] = permissions.getSatisfiedFactors();
const missing: Factor[] = permissions.getMissingFactors(Permission.DownloadReports);

// TypeScript enforces Permission type
permissions.checkPermission(Permission.ViewDashboard); // OK
permissions.checkPermission(999); // OK (number), but semantically wrong
```

## Caching

The library automatically caches permission check results:

```typescript
const permissions = new FactorBasedPermissions(serialized);

// First call: computes and caches result
permissions.checkPermission(Permission.ViewDashboard);

// Subsequent calls: returns cached result (O(1))
permissions.checkPermission(Permission.ViewDashboard);
permissions.checkPermission(Permission.ViewDashboard);
```

## Important Notes

### This Library Does NOT Serialize

This is a **read-only** library. It parses and checks permissions but cannot create or modify them. Serialization should only happen on the server (C#) where the source of truth for factors and permissions resides.

```typescript
// ❌ Not supported
permissions.addFactor(Factor.EmailVerified);
permissions.serialize();

// ✅ Correct pattern
// 1. Client requests server to perform action
// 2. Server updates factors, creates new permissions
// 3. Server issues new JWT with updated "ap" claim
// 4. Client receives new JWT and re-creates FactorBasedPermissions
```

### Always Validate on Server

Client-side permission checks are for **UI purposes only**. Always validate permissions on the server before performing sensitive operations:

```typescript
// Client: Show/hide UI based on permissions
if (permissions.checkPermission(Permission.DeleteUser)) {
  showDeleteButton();
}

// Server: ALWAYS verify before actually deleting
app.delete("/users/:id", authorize(Permission.DeleteUser), (req, res) => {
  // Server-side check happens in authorize middleware
  deleteUser(req.params.id);
});
```

## Browser Support

- All modern browsers (ES2015+)
- Node.js 14+

## Bundle Size

~1KB minified (no dependencies)

## License

MIT
