# Identity Database (IdentityDb)

> Database schema for Identity.API managing users, roles, and ASP.NET Core Identity security claims.

---

## Entity Relationship Diagram

The diagram below contains the raw schema generated from DataGrip with physical foreign keys, alongside manual overlay stubs indicating which external services reference `AspNetUsers`.

```mermaid
classDiagram
direction BT
%% Last verified against commit: b5229aa

%% --- GENERATED START --- (copied from generated/identity.mmd, do not edit)
class AspNetRoleClaims {
   uuid RoleId
   text ClaimType
   text ClaimValue
   integer Id
}
class AspNetRoles {
   varchar(256) Name
   varchar(256) NormalizedName
   text ConcurrencyStamp
   uuid Id
}
class AspNetUserClaims {
   uuid UserId
   text ClaimType
   text ClaimValue
   integer Id
}
class AspNetUserLogins {
   text ProviderDisplayName
   uuid UserId
   text LoginProvider
   text ProviderKey
}
class AspNetUserRoles {
   uuid UserId
   uuid RoleId
}
class AspNetUserTokens {
   text Value
   uuid UserId
   text LoginProvider
   text Name
}
class AspNetUsers {
   timestamp with time zone CreatedDate
   timestamp with time zone UpdatedDate
   varchar(50) FirstName
   varchar(50) LastName
   varchar(256) UserName
   varchar(256) NormalizedUserName
   varchar(256) Email
   varchar(256) NormalizedEmail
   boolean EmailConfirmed
   text PasswordHash
   text SecurityStamp
   text ConcurrencyStamp
   text PhoneNumber
   boolean PhoneNumberConfirmed
   boolean TwoFactorEnabled
   timestamp with time zone LockoutEnd
   boolean LockoutEnabled
   integer AccessFailedCount
   uuid Id
}
class __EFMigrationsHistory {
   varchar(32) ProductVersion
   varchar(150) MigrationId
}

AspNetRoleClaims  -->  AspNetRoles : RoleId -> Id
AspNetUserClaims  -->  AspNetUsers : UserId -> Id
AspNetUserLogins  -->  AspNetUsers : UserId -> Id
AspNetUserRoles  -->  AspNetRoles : RoleId -> Id
AspNetUserRoles  -->  AspNetUsers : UserId -> Id
AspNetUserTokens  -->  AspNetUsers : UserId -> Id
%% --- GENERATED END ---

%% --- MANUAL OVERLAY START ---
class Wallets {
    uuid UserId "stub: owned by Portfolio.API"
}
class LimitOrders {
    uuid UserId "stub: owned by Portfolio.API"
}
class Notifications {
    uuid UserId "stub: owned by Notifications.API"
}
class PriceAlerts {
    uuid UserId "stub: owned by Notifications.API"
}

Wallets ..> AspNetUsers : logical (Wallets.UserId -> Id)
LimitOrders ..> AspNetUsers : logical (LimitOrders.UserId -> Id)
Notifications ..> AspNetUsers : logical (Notifications.UserId -> Id)
PriceAlerts ..> AspNetUsers : logical (PriceAlerts.UserId -> Id)
%% --- MANUAL OVERLAY END ---
```

*Solid arrows (`-->`) denote physical database foreign keys; dashed arrows (`..>`) denote logical cross-service references.*

---

## Constraints, Indexes & Defaults

| Table | Constraint / Index Name | Type | Column(s) | Description | Configuration Source |
|---|---|---|---|---|---|
| `AspNetUsers` | `UserNameIndex` | Unique Index | `NormalizedUserName` | Guarantees username uniqueness | `Identity.API/Migrations/...ModelSnapshot.cs` |
| `AspNetUsers` | `EmailIndex` | Unique Index | `NormalizedEmail` | Guarantees email uniqueness | `Identity.API/Migrations/...ModelSnapshot.cs` |
| `AspNetUsers` | `FirstName` | Column Constraint | `FirstName` | Required, max length 50 | `Identity.API/Infrastructure/Configurations/UserConfiguration.cs` |
| `AspNetUsers` | `LastName` | Column Constraint | `LastName` | Required, max length 50 | `Identity.API/Infrastructure/Configurations/UserConfiguration.cs` |
| `AspNetUsers` | `CreatedDate` | Column Constraint | `CreatedDate` | Required timestamp | `Identity.API/Infrastructure/Configurations/UserConfiguration.cs` |
| `AspNetRoles` | `RoleNameIndex` | Unique Index | `NormalizedName` | Guarantees role name uniqueness | `Identity.API/Migrations/...ModelSnapshot.cs` |

---

## Logical Cross-Service References

`AspNetUsers.Id` serves as the primary user identity token across the entire microservices architecture. It is referenced logically without physical foreign keys by:

| Referencing Service | Referencing Table & Column | Cardinality | Consistency Mechanism | If User Deleted |
|---|---|---|---|---|
| `Portfolio.API` | `Wallets.UserId` | 1:1 | Created asynchronously via `UserCreatedEvent` on registration | Orphaned wallet |
| `Portfolio.API` | `LimitOrders.UserId` | 1:N | Derived from authenticated JWT claims | Orphaned limit orders |
| `Notifications.API` | `Notifications.UserId` | 1:N | Set during notification creation from event payloads | Orphaned notifications |
| `Notifications.API` | `PriceAlerts.UserId` | 1:N | Derived from authenticated JWT claims | Orphaned alerts |

---

## Sources

- `Identity.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Identity.API/Infrastructure/Configurations/UserConfiguration.cs`
- `Identity.API/Migrations/ApplicationDbContextModelSnapshot.cs`
- `docs/database/generated/identity.mmd`
- `docs/_inventory.md` — Section 2 (Databases)
