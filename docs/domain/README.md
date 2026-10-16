# Domain Models and State Machines

> Domain diagrams, aggregate roots, state lifecycle machines, and service contracts for CryptoMarket backend microservices.

---

## Domain Catalog

| Domain Model | Type | Document | Key Entities / Components | Description |
|---|---|---|---|---|
| Limit Order Lifecycle | State Machine | [limit-order-state-machine.md](limit-order-state-machine.md) | `LimitOrder`, `LimitOrderStatus`, `LimitOrderService` | State transitions (`Pending` -> `Processing` -> `Filled`), guard conditions, and cancellation behavior |
| Portfolio Domain | Class Diagram | [portfolio-class-diagram.md](portfolio-class-diagram.md) | `Wallet`, `Asset`, `LimitOrder`, `Transaction`, `WalletService`, `LimitOrderService` | Aggregate root relationships, invariants, maker/taker fee handling, and concurrency tokens |
| Market Domain | Class Diagram | [market-class-diagram.md](market-class-diagram.md) | `Coin`, `PriceHistory`, `MarketNews`, `PriceSimulation`, `CoinService`, `IGeminiService` | Asset pricing anchors, OHLCV time-series history, AI market news generation, and simulation workers |
| Identity Domain | Class Diagram | [identity-class-diagram.md](identity-class-diagram.md) | `AppUser`, `AppRole`, `IUserService`, `IAuthenticationService` | User aggregate root, identity roles, and authentication service contracts |
| Notifications Domain | Class Diagram | [notifications-class-diagram.md](notifications-class-diagram.md) | `Notification`, `PriceAlert`, `INotificationService`, `IPriceAlertService`, `NotificationHub` | Notification read state lifecycle, price alert threshold triggers, and real-time push contracts |

---

## Domain Architecture Principles

1. **Independent Service Schemas:** Each microservice maintains its own domain boundary and database schema without physical foreign key cross-dependencies.
2. **Aggregate Root Boundaries:** In `Portfolio.API`, the `Wallet` entity controls child `Asset` balances and encapsulates mutation operations to protect consistency.
3. **Optimistic Concurrency:** Concurrent mutations against wallet balances are guarded via the `RowVersion` uint concurrency token.
4. **State Machine Verification:** Limit order executions transition through atomic states in Redis and PostgreSQL to prevent race conditions during parallel processing.

---

## Sources

- [limit-order-state-machine.md](file:///c:/CryptoMarketProject/backend/docs/domain/limit-order-state-machine.md)
- [portfolio-class-diagram.md](file:///c:/CryptoMarketProject/backend/docs/domain/portfolio-class-diagram.md)
- [market-class-diagram.md](file:///c:/CryptoMarketProject/backend/docs/domain/market-class-diagram.md)
- [identity-class-diagram.md](file:///c:/CryptoMarketProject/backend/docs/domain/identity-class-diagram.md)
- [notifications-class-diagram.md](file:///c:/CryptoMarketProject/backend/docs/domain/notifications-class-diagram.md)

