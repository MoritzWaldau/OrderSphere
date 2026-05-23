# Role Model

All roles are defined in `contracts/keycloak/ordersphere-realm.json` and are the authoritative source. This document maps realm roles to ASP.NET authorization policies and ABAC handlers.

## Realm Roles

| Role | Type | Assigned to | Effective permissions |
|---|---|---|---|
| `customer` | Simple | Every end-user (Keycloak default role) | Own cart, own orders, own profile |
| `csr` | Composite (`requires-mfa`) | Customer Service staff | Read all orders and customer data; no writes |
| `order-manager` | Composite (`requires-mfa`) | Operations staff | Update order status, cancel orders |
| `catalog-admin` | Composite (`requires-mfa`) | Merchandising staff | Full CRUD on catalog products and categories |
| `admin` | Composite (`csr` + `order-manager` + `catalog-admin` + `requires-mfa`) | Platform administrators | All of the above |
| `requires-mfa` | Simple (gating only) | Inherited via `csr`, `order-manager`, `catalog-admin`, `admin` | Triggers MFA second factor in browser flow |
| `svc.ordering` | Simple (machine) | `ordering-worker` service account | Machine identity for Catalog M2M calls |
| `svc.notification` | Simple (machine) | `notification-worker` service account | Machine identity for future UserProfile M2M calls |

### Composite Role Inheritance

```
admin
├── csr         ──► requires-mfa
├── order-manager ► requires-mfa
├── catalog-admin ► requires-mfa
└── requires-mfa (direct)
```

A user with `admin` role effectively has all four child roles. MFA is triggered for any user who effectively holds `requires-mfa` via the conditional browser authentication flow.

---

## ASP.NET Authorization Policies

### Ordering.Api (`src/Services/Ordering/OrderSphere.Ordering.Api/`)

| Policy constant | Definition | File |
|---|---|---|
| `AuthorizationPolicies.Admin` | `RequireRole("admin")` | `Configuration/AuthorizationExtensions.cs` |
| `AuthorizationPolicies.Staff` | `RequireRole("csr", "order-manager", "admin")` | same |
| `AuthorizationPolicies.OrderManager` | `RequireRole("order-manager", "admin")` | same |
| `AuthorizationPolicies.OrderOwnerOrStaff` | Resource-based ABAC | same |

### Catalog.Api (`src/Services/Catalog/OrderSphere.Catalog.Api/`)

| Policy constant | Definition | File |
|---|---|---|
| `AuthorizationPolicies.CatalogAdmin` | `RequireRole("catalog-admin", "admin")` | `Configuration/AuthorizationExtensions.cs` |

### UserProfile.Api (`src/Services/UserProfile/OrderSphere.UserProfile.Api/`)

| Policy constant | Definition | File |
|---|---|---|
| `AuthorizationPolicies.Admin` | `RequireRole("admin")` | `Configuration/AuthorizationExtensions.cs` |

### BFF (`src/Gateways/OrderSphere.Bff/`)

| Policy | Definition | Applied to |
|---|---|---|
| `BffUserPolicy` | `RequireAuthenticatedUser()` | All `/api/**` reverse-proxy routes |

---

## ABAC Handlers

Resource-based access control is implemented as `IAuthorizationHandler<TRequirement, TResource>` instances.

### `OrderOwnerOrStaffHandler`

**File**: `src/Services/Ordering/OrderSphere.Ordering.Api/Authorization/OrderOwnerOrStaffHandler.cs`

**Logic**:
1. If `User.FindFirst("sub").Value == order.CustomerId` → **pass** (owner).
2. If `User.IsInRole("csr") || User.IsInRole("order-manager") || User.IsInRole("admin")` → **pass** (staff).
3. Otherwise → **fail** (403).

**Usage pattern** in endpoint handlers:
```csharp
var authResult = await authorizationService.AuthorizeAsync(
    User, order, AuthorizationPolicies.OrderOwnerOrStaff);
if (!authResult.Succeeded)
    return Results.Forbid();
```

---

## MFA Conditional Flow

Staff users (those with `requires-mfa` role via composite inheritance) are required to complete a second factor after entering their password. The conditional browser authentication flow is defined in the realm JSON under `authenticationFlows`.

**Flow**: `browser-with-conditional-otp`

```
Cookie check           ──► ALTERNATIVE (skip if valid cookie)
IdP Redirector         ──► ALTERNATIVE
browser-conditional-otp-forms (sub-flow)
  Username + Password  ──► REQUIRED
  browser-conditional-otp-2fa (CONDITIONAL sub-flow)
    Condition: user has role 'requires-mfa'   ──► REQUIRED (gates sub-flow)
    WebAuthn Authenticator                    ──► ALTERNATIVE
    OTP Form (TOTP)                           ──► ALTERNATIVE
```

Regular `customer` users complete login after password only (the conditional sub-flow is skipped because they do not hold `requires-mfa`).

---

## Token Claims

The `roles` claim in issued access tokens contains the user's effective realm roles (flattened composite tree). Services configure `RoleClaimType = "roles"` in `AddOrderSphereJwtAuth()` so `User.IsInRole(...)` reads from this claim directly.

Example access token payload:
```json
{
  "sub": "e5f6a7b8-...",
  "preferred_username": "moritz.waldau@ordersphere.dev",
  "email": "moritz.waldau@ordersphere.dev",
  "roles": ["admin", "customer", "csr", "order-manager", "catalog-admin", "requires-mfa"],
  "aud": "ordering-api",
  "iss": "http://localhost:8080/realms/ordersphere",
  "exp": 1748000000
}
```
