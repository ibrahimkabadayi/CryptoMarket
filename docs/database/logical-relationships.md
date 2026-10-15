# Cross-Service Logical Relationships

> Catalog of all cross-service logical relationships that link independent databases without physical foreign key constraints.

---

## Overview Diagram

Because each microservice owns an isolated PostgreSQL database, relationships between domains are enforced at the application level and mediated asynchronously via RabbitMQ events.

The diagram below displays the platform's primary domain entities across all 4 databases, highlighting **only cross-service logical relationships** as dashed lines (`..>`):

```mermaid
classDiagram
direction TB
%% Last verified against commit: b5229aa

    subgraph Identity_Domain ["Identity.API (IdentityDb)"]
        class AspNetUsers {
            uuid Id PK
            varchar UserName
            varchar Email
        }
    end

    subgraph Market_Domain ["Market.API (MarketDb)"]
        class Coins {
            uuid Id PK
            varchar Symbol UK
            varchar Name
            numeric CurrentPrice
        }
    end

    subgraph Portfolio_Domain ["Portfolio.API (PortfolioDb)"]
        class Wallets {
            uuid Id PK
            uuid UserId
            text Address
            numeric FiatBalance
        }
        class Assets {
            uuid Id PK
            uuid WalletId FK
            varchar Symbol
            numeric Quantity
        }
        class LimitOrders {
            uuid Id PK
            uuid WalletId FK
            uuid UserId
            text Symbol
            numeric TargetPrice
        }
        class Transactions {
            uuid Id PK
            uuid WalletId FK
            varchar Symbol
            numeric Amount
        }
        class TreasuryBalances {
            uuid Id PK
            varchar AssetSymbol
            numeric TotalAmount
        }
    end

    subgraph Notifications_Domain ["Notifications.API (NotificationsDb)"]
        class Notifications {
            uuid Id PK
            uuid UserId
            text Title
            text Message
        }
        class PriceAlerts {
            uuid Id PK
            uuid UserId
            text Symbol
            numeric TargetPrice
        }
    end

    %% User Identity Logical References
    Wallets ..> AspNetUsers : "UserId -> Id"
    LimitOrders ..> AspNetUsers : "UserId -> Id"
    Notifications ..> AspNetUsers : "UserId -> Id"
    PriceAlerts ..> AspNetUsers : "UserId -> Id"

    %% Coin Symbol Logical References
    Assets ..> Coins : "Symbol -> Symbol"
    LimitOrders ..> Coins : "Symbol -> Symbol"
    Transactions ..> Coins : "Symbol -> Symbol"
    TreasuryBalances ..> Coins : "AssetSymbol -> Symbol"
    PriceAlerts ..> Coins : "Symbol -> Symbol"
```

*All dashed lines (`..>`) represent logical references between independent databases. No physical database foreign key constraints exist across services.*

---

## Logical Relationships Catalog

| Source Column (service.table.column) | Target Entity (service.table.column) | Cardinality | How Consistency is Maintained | If Target is Deleted | Verified In (Code File) |
|---|---|---|---|---|---|
| `Portfolio.Wallets.UserId` | `Identity.AspNetUsers.Id` | 1:1 | Created asynchronously by `UserCreatedConsumer` upon receiving `UserCreatedEvent`. Unique index enforced on `UserId`. | Wallet becomes orphaned; no cascade delete. | `Portfolio.API/Consumers/UserCreatedConsumer.cs` |
| `Portfolio.LimitOrders.UserId` | `Identity.AspNetUsers.Id` | 1:N | Derived directly from verified JWT claims in `LimitOrderController.CreateLimitOrder`. | Limit order remains in database; order will not execute if wallet inaccessible. | `Portfolio.API/Controllers/LimitOrderController.cs` |
| `Notifications.Notifications.UserId` | `Identity.AspNetUsers.Id` | 1:N | Extracted from event payloads (`UserCreatedEvent`, `AssetTransferEvent`, `LimitOrderOccuredEvent`) or JWT. | Historical notifications remain orphaned. | `Notifications.API/Consumers/*.cs` |
| `Notifications.PriceAlerts.UserId` | `Identity.AspNetUsers.Id` | 1:N | Derived directly from verified JWT claims in `PriceAlertController.CreateAlert`. | Alert remains in DB; cannot be triggered or managed. | `Notifications.API/Controllers/PriceAlertController.cs` |
| `Portfolio.Assets.Symbol` | `Market.Coins.Symbol` | 1:N | Validated during purchase/trade execution against current market coins. | Asset holding retains symbol; price calculations fallback to last known or fail. | `Portfolio.API/Application/Services/WalletService.cs` |
| `Portfolio.LimitOrders.Symbol` | `Market.Coins.Symbol` | 1:N | Validated during limit order creation; matched against `CoinPriceEvent` streams. | Limit order remains `Pending` indefinitely since no matching price events arrive. | `Portfolio.API/Application/Services/LimitOrderService.cs` |
| `Portfolio.Transactions.Symbol` | `Market.Coins.Symbol` | 1:N | Snapshot recorded at trade execution time for audit history. | Historical transaction log record preserved unaltered. | `Portfolio.API/Application/Services/TransactionService.cs` |
| `Portfolio.TreasuryBalances.AssetSymbol` | `Market.Coins.Symbol` | 1:1 | Platform fees credited in coin currency via `TreasureBalanceService.AddFee`. | Platform treasury fee balance preserved. | `Portfolio.API/Application/Services/TreasureBalanceService.cs` |
| `Notifications.PriceAlerts.Symbol` | `Market.Coins.Symbol` | 1:N | Evaluated against `CoinPriceEvent.Symbol` streamed from `Market.API`. | Alert remains `Active` indefinitely since matching price updates will not arrive. | `Notifications.API/Application/Services/PriceAlertService.cs` |

---

## Sources

- `Identity.API/Infrastructure/Configurations/UserConfiguration.cs`
- `Market.API/Infrastructure/Configurations/CoinConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/LimitOrderConfiguration.cs`
- `Portfolio.API/Infrastructure/Configurations/AssetConfiguration.cs`
- `Notifications.API/Domain/Entities/PriceAlert.cs`
- `docs/_inventory.md` — Section 2 (Databases per service)
