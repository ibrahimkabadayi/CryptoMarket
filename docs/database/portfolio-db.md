# Portfolio Database (PortfolioDb)

> Database schema for Portfolio.API managing user wallets, crypto asset holdings, transaction history, limit orders, and platform treasury balances.

---

## Entity Relationship Diagram

The diagram below contains the raw schema generated from DataGrip with physical foreign keys, alongside manual overlay stubs indicating logical cross-service relationships to `IdentityDb` and `MarketDb`.

```mermaid
classDiagram
direction BT
%% Last verified against commit: b5229aa

%% --- GENERATED START --- (copied from generated/portfolio.mmd, do not edit)
class Assets {
   uuid WalletId
   varchar(50) Symbol
   numeric CostBasis
   numeric Quantity
   timestamp with time zone CreatedDate
   timestamp with time zone UpdatedDate
   uuid Id
}
class LimitOrders {
   uuid WalletId
   text Symbol
   numeric Amount
   integer OrderType
   timestamp with time zone CreatedDate
   timestamp with time zone UpdatedDate
   integer OrderStatus
   numeric TargetPrice
   uuid UserId
   uuid Id
}
class Transactions {
   uuid WalletId
   uuid AssetId
   varchar(50) Symbol
   numeric Amount
   numeric PriceAtTransaction
   integer TransactionType
   timestamp with time zone CreatedDate
   timestamp with time zone UpdatedDate
   uuid Id
}
class TreasuryBalances {
   varchar(10) AssetSymbol
   numeric(18,8) TotalAmount
   timestamp with time zone CreatedDate
   timestamp with time zone UpdatedDate
   uuid Id
}
class Wallets {
   uuid UserId
   text Address
   numeric FiatBalance
   numeric Value
   timestamp with time zone CreatedDate
   timestamp with time zone UpdatedDate
   uuid Id
}
class __EFMigrationsHistory {
   varchar(32) ProductVersion
   varchar(150) MigrationId
}

Assets  -->  Wallets : WalletId -> Id
LimitOrders  -->  Wallets : WalletId -> Id
Transactions  -->  Wallets : WalletId -> Id
%% --- GENERATED END ---

%% --- MANUAL OVERLAY START ---
class AspNetUsers {
    uuid Id "stub: owned by Identity.API"
}
class Coins {
    varchar(10) Symbol "stub: owned by Market.API"
}

Wallets ..> AspNetUsers : logical (Wallets.UserId -> AspNetUsers.Id)
LimitOrders ..> AspNetUsers : logical (LimitOrders.UserId -> AspNetUsers.Id)
Assets ..> Coins : logical (Assets.Symbol -> Coins.Symbol)
LimitOrders ..> Coins : logical (LimitOrders.Symbol -> Coins.Symbol)
Transactions ..> Coins : logical (Transactions.Symbol -> Coins.Symbol)
TreasuryBalances ..> Coins : logical (TreasuryBalances.AssetSymbol -> Coins.Symbol)
%% --- MANUAL OVERLAY END ---
```

*Solid arrows (`-->`) denote physical database foreign keys; dashed arrows (`..>`) denote logical cross-service references.*

---

## Constraints, Indexes & Defaults

| Table | Constraint / Index Name | Type | Column(s) | Description | Configuration Source |
|---|---|---|---|---|---|
| `Wallets` | `IX_Wallets_UserId` | Unique Index | `UserId` | Enforces one wallet per registered user | `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs` |
| `Wallets` | `Address` | Column Constraint | `Address` | Required generated hexadecimal address | `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs` |
| `Wallets` | `FiatBalance` | Default Value | `FiatBalance` | Defaults to `0`, required numeric | `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs` |
| `Assets` | `FK_Assets_Wallets_WalletId` | Foreign Key | `WalletId` | Restricts wallet deletion if assets exist (`DeleteBehavior.Restrict`) | `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs` |
| `Assets` | `Symbol` | Column Constraint | `Symbol` | Required, max length 50 | `Portfolio.API/Infrastructure/Configurations/AssetConfiguration.cs` |
| `LimitOrders` | `FK_LimitOrders_Wallets_WalletId`| Foreign Key | `WalletId` | Linked wallet foreign key (`DeleteBehavior.NoAction`) | `Portfolio.API/Infrastructure/Configurations/LimitOrderConfiguration.cs` |
| `LimitOrders` | `Symbol`, `Amount`, `TargetPrice`| Column Constraint | Columns | Required order parameters | `Portfolio.API/Infrastructure/Configurations/LimitOrderConfiguration.cs` |
| `Transactions` | `FK_Transactions_Wallets_WalletId`| Foreign Key | `WalletId` | Restricts wallet deletion if transactions exist (`DeleteBehavior.Restrict`) | `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs` |
| `TreasuryBalances`| `AssetSymbol` | Column Constraint | `AssetSymbol` | Required, max length 10 | `Portfolio.API/Infrastructure/Configurations/TreasuryBalanceConfiguration.cs` |
| `TreasuryBalances`| `TotalAmount` | Precision | `TotalAmount` | `decimal(18,8)` | `Portfolio.API/Infrastructure/Configurations/TreasuryBalanceConfiguration.cs` |

---

## Logical Cross-Service References

| Local Table & Column | Target Service & Entity | Cardinality | Consistency Mechanism | If Target Deleted |
|---|---|---|---|---|
| `Wallets.UserId` | `Identity.API: AspNetUsers.Id` | 1:1 | Created asynchronously upon receiving `UserCreatedEvent` | Orphaned wallet |
| `LimitOrders.UserId` | `Identity.API: AspNetUsers.Id` | 1:N | Derived from authenticated JWT claims | Orphaned order |
| `Assets.Symbol` | `Market.API: Coins.Symbol` | 1:N | Validated during purchase/deposit against active coins | Orphaned asset balance |
| `LimitOrders.Symbol` | `Market.API: Coins.Symbol` | 1:N | Validated against market coins during order creation | Order will never match |
| `Transactions.Symbol` | `Market.API: Coins.Symbol` | 1:N | Recorded from executed trade operation | Historical audit log retained |
| `TreasuryBalances.AssetSymbol` | `Market.API: Coins.Symbol` | 1:1 | Accumulated platform fee currency | Fee balance retained |

---

## Sources

- `Portfolio.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/AssetConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/LimitOrderConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/TransactionConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/TreasuryBalanceConfiguration.cs`
- `docs/database/generated/portfolio.mmd`
- `docs/_inventory.md` — Section 2 (Databases)
