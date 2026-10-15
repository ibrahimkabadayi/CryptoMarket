# Event Catalog

> Complete catalog of message contracts, publishers, consumers, queue bindings, duplicate-handling mechanisms, and retry behavior across the CryptoMarket messaging bus.

---

## Event Catalog

All message contracts are defined as immutable C# records in the `Shared.Messages` project and published via MassTransit over RabbitMQ.

| Event | Producer | Consumer(s) | Fields | Idempotency Key | Failure / Retry Behavior | Source |
|---|---|---|---|---|---|---|
| `UserCreatedEvent` | `Identity.API`<br/>(`UserService.AddUserAsync`) | 1. `Portfolio.API`<br/>(`portfolio-user-created-queue`)<br/>2. `Notifications.API`<br/>(`notification-user-created-queue`) | `Guid UserId`<br/>`string Email`<br/>`string UserName` | None | Default MassTransit retry; unhandled faults route to `_error` queues | `Shared.Messages/UserCreatedEvent.cs`<br/>`Identity.API/Application/Services/UserService.cs` |
| `CoinPriceEvent` | `Market.API`<br/>(`PriceSimulation.ExecuteAsync`) | 1. `Portfolio.API`<br/>(`portfolio-coin-price-queue`)<br/>2. `Notifications.API`<br/>(`notification-coin-price-queue`) | `string Symbol`<br/>`decimal Price` | None (ephemeral real-time ticks) | Faults route to `_error` queues | `Shared.Messages/CoinPriceEvent.cs`<br/>`Market.API/Infrastructure/BackgroundServices/PriceSimulation.cs` |
| `LimitOrderPlacedEvent` | `Market.API`<br/>(`LimitOrderService.SetLimitOrder`) | `Portfolio.API`<br/>(`portfolio-set-limit-order-queue`) | `Guid CorrelationId`<br/>`Guid UserId`<br/>`string Symbol`<br/>`decimal TargetPrice`<br/>`decimal Amount`<br/>`string OrderType` | `Guid CorrelationId` (cached in Redis, 24h TTL) | Duplicate throws `InvalidOperationException`; faults route to `_error` queue | `Shared.Messages/LimitOrderPlacedEvent.cs`<br/>`Market.API/Application/Services/LimitOrderService.cs` |
| `LimitOrderOccuredEvent` | `Portfolio.API`<br/>(`LimitOrderService.ApplyLimitOrder`) | `Notifications.API`<br/>(`notification-limit-order-queue`) | `Guid UserId`<br/>`string Symbol`<br/>`decimal Price`<br/>`decimal Amount`<br/>`DateTime Date`<br/>`string Ordertype` | None | Faults route to `notification-limit-order-queue_error` | `Shared.Messages/LimitOrderOccuredEvent.cs`<br/>`Portfolio.API/Application/Services/LimitOrderService.cs` |
| `AssetTransferEvent` | `Portfolio.API`<br/>(`WalletService.TransferAsset`) | `Notifications.API`<br/>(`notification-asset-transferred-queue`) | `string Message`<br/>`string Symbol`<br/>`decimal Quantity`<br/>`Guid SourceWalletUserId`<br/>`Guid TargetWalletUserId` | None | Faults route to `notification-asset-transferred-queue_error` | `Shared.Messages/AssetTransferEvent.cs`<br/>`Portfolio.API/Application/Services/WalletService.cs` |
| `BuyCoinEvent` | `Market.API`<br/>(`CoinService.BuyCoin`) | `Portfolio.API`<br/>(`portfolio-buy-coin-order-queue`) | `Guid UserId`<br/>`string Symbol`<br/>`decimal BuyPrice`<br/>`decimal BuyAmount` | None | Faults route to `portfolio-buy-coin-order-queue_error` | `Shared.Messages/BuyCoinEvent.cs`<br/>`Market.API/Application/Services/CoinService.cs` |
| `FeeCollectionEvent` | `Portfolio.API`<br/>(`WalletService.BuyAsset`, `SellAsset`) | `Portfolio.API`<br/>(`Consumers/FeeCollectionConsumer.cs`) | `string Symbol`<br/>`decimal FeeAmount`<br/>`Guid UserId`<br/>`DateTime OccurredOn` | None | Faults route to `_error` queue | `Shared.Messages/FeeCollectionEvent.cs`<br/>`Portfolio.API/Application/Services/WalletService.cs` |
| `AssetDeposited` | `Portfolio.API`<br/>(`WalletService.DepositMoney`) | External consumers (handled outside backend) | `Guid WalletId`<br/>`decimal Amount`<br/>`string Currency`<br/>`DateTime OccurredAt` | None | Delivered to external queue bindings | `Shared.Messages/AssetDeposited.cs`<br/>`Portfolio.API/Application/Services/WalletService.cs` |
| `AssetWithdrawn` | `Portfolio.API`<br/>(`WalletService.WithdrawMoney`) | External consumers (handled outside backend) | `Guid WalletId`<br/>`decimal Amount`<br/>`string Currency`<br/>`DateTime OccurredAt` | None | Delivered to external queue bindings | `Shared.Messages/AssetWithdrawn.cs`<br/>`Portfolio.API/Application/Services/WalletService.cs` |
| `TradeExecuted` | `Portfolio.API`<br/>(`WalletService.BuyAsset`, `SellAsset`) | External consumers (handled outside backend) | `Guid WalletId`<br/>`decimal BoughtAmount`<br/>`string BoughtCurrency`<br/>`decimal SoldAmount`<br/>`string SoldCurrency`<br/>`DateTime OccurredAt` | None | Delivered to external queue bindings | `Shared.Messages/TradeExecuted.cs`<br/>`Portfolio.API/Application/Services/WalletService.cs` |
| `UserAlreadyRegistiredEvent` | None (Unused) | None | `string UserName`<br/>`string UserEmail` | N/A | N/A | `Shared.Messages/UserAlreadyRegistiredEvent.cs` |

---

## Consumer Duplicate-Message Checks (ADR 0004 Audit)

The table below documents whether each active consumer implements duplicate-message checking via Redis cache caching by `CorrelationId` or alternative mechanisms:

| Consumer Class | Service | Bound Queue Name | Message Type | Duplicate Check by CorrelationId? | Exact Mechanism & TTL |
|---|---|---|---|---|---|
| `SetLimitOrderEvent` | `Portfolio.API` | `portfolio-set-limit-order-queue` | `LimitOrderPlacedEvent` | **Yes** | `LimitOrderService.CreateLimitOrderAsync` queries Redis key `{correlationId}`. If present, execution aborts with `InvalidOperationException`. After successful database insertion, `{correlationId}` is stored in Redis with a 24-hour TTL (`TimeSpan.FromHours(24)`). |
| `UserCreatedConsumer` | `Portfolio.API` | `portfolio-user-created-queue` | `UserCreatedEvent` | **No** | Wallet created directly. Uniqueness is enforced at the database level via the unique index on `Wallets.UserId`. |
| `BuyCoinConsumer` | `Portfolio.API` | `portfolio-buy-coin-order-queue` | `BuyCoinEvent` | **No** | Directly executes `BuyAssetWithUserId`. |
| `CoinPriceConsumer` | `Portfolio.API` | `portfolio-coin-price-queue` | `CoinPriceEvent` | **No** | Evaluates price against active cached limit orders; naturally idempotent read check. |
| `UserCreatedConsumer` | `Notifications.API` | `notification-user-created-queue` | `UserCreatedEvent` | **No** | Directly dispatches welcome email via SMTP. |
| `CoinPriceConsumer` | `Notifications.API` | `notification-coin-price-queue` | `CoinPriceEvent` | **No** | Evaluates price against cached active alerts; deactivates alert when triggered. |
| `LimitOrderConsumer` | `Notifications.API` | `notification-limit-order-queue` | `LimitOrderOccuredEvent` | **No** | Creates in-app notification row in database directly. |
| `AssetTransferConsumer` | `Notifications.API` | `notification-asset-transferred-queue` | `AssetTransferEvent` | **No** | Creates in-app notification row in database directly. |

---

## RabbitMQ Topology Details

- **Broker Exchange Type:** MassTransit provisions `fanout` topic exchanges named after the fully-qualified .NET type name (e.g. `Shared.Messages:CoinPriceEvent`).
- **Endpoint Binding:** Receive endpoints bind their designated queues to the appropriate message exchange.
- **Dead-Letter / Fault Queues:** When a consumer throws an unhandled exception, MassTransit creates a fault message and routes the failed payload to an error queue suffixed with `_error` (e.g. `notification-limit-order-queue_error`).
- **Outbox Pattern:** Transactional outbox is not configured; messages are published directly to the broker during service execution.

---

## Sources

- `Shared.Messages/*.cs`
- `Identity.API/Program.cs`
- `Market.API/Program.cs`
- `Portfolio.API/Program.cs`
- `Notifications.API/Program.cs`
- `Portfolio.API/Consumers/*.cs`
- `Notifications.API/Consumers/*.cs`
- `Portfolio.API/Application/Services/LimitOrderService.cs`
- `docs/decisions/_owner-notes.md` — Section 0004
