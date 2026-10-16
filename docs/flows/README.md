# System Flows and Sequence Diagrams

> End-to-end sequence diagrams documenting key asynchronous, event-driven, and synchronous workflows across CryptoMarket services.

All sequence diagrams in this section map exclusively to concrete classes, controllers, domain services, messaging consumers, and database repositories present in the codebase.

---

## Flow Index

| # | Flow | Document | Key Participants | Alternative & Failure Paths Shown |
|---|---|---|---|---|
| 1 | Create Limit Order | [create-limit-order.md](create-limit-order.md) | `MarketController`, `LimitOrderPlacedEvent`, `SetLimitOrderEvent`, `LimitOrderService`, Redis, `LimitOrderRepository` | Missing JWT claims, unparsable `OrderType` enum, duplicate `CorrelationId` in Redis, broker outage |
| 2 | Limit Order Triggered | [limit-order-triggered.md](limit-order-triggered.md) | `PriceSimulation`, `CoinPriceEvent`, `CoinPriceConsumer`, `LimitOrderService`, `WalletService`, `PortfolioHub`, `LimitOrderConsumer`, `NotificationHub` | Price cache miss/hit, insufficient fiat balance, unfulfilled limit order, offline SignalR push |
| 3 | Buy Coin | [buy-coin.md](buy-coin.md) | `MarketController`, `CoinService`, `BuyCoinEvent`, `BuyCoinConsumer`, `WalletService`, `WalletRepository`, `PortfolioHub`, `IdempotencyMiddleware` | Missing user identity claim, missing wallet, insufficient fiat balance, duplicate idempotency key |
| 4 | Transfer Asset | [transfer-asset.md](transfer-asset.md) | `IdempotencyMiddleware`, `WalletController`, `WalletService`, `WalletRepository`, `AssetTransferEvent`, `AssetTransferConsumer`, `NotificationHub` | Missing or duplicate `Idempotency-Key` header, invalid destination wallet address, unowned asset symbol, insufficient crypto balance |
| 5 | Dashboard Read | [dashboard-read.md](dashboard-read.md) | `WalletController`, `WalletService`, `WalletRepository`, `TransactionService`, `CoinService`, `CoinRepository`, Redis (`market:coins`) | Missing/invalid JWT token, unregistered user wallet, Redis cache hit vs. cache miss with database fallback |

---

## Conventions

- Every sequence diagram includes the verification stamp `%% Last verified against commit: b5229aa`.
- Success paths and multiple failure/alternative paths are modeled via `alt` and `opt` blocks.
- Parallel background processing is modeled with `par` blocks corresponding to `Parallel.ForEachAsync`.
- All message published arrows correspond directly to contracts in `Shared.Messages`.

---

## Sources

- `CRYPTOMARKET_DOCS_IMPLEMENTATION_PLAN.md` — Phase 5 requirements
- [create-limit-order.md](file:///c:/CryptoMarketProject/backend/docs/flows/create-limit-order.md)
- [limit-order-triggered.md](file:///c:/CryptoMarketProject/backend/docs/flows/limit-order-triggered.md)
- [buy-coin.md](file:///c:/CryptoMarketProject/backend/docs/flows/buy-coin.md)
- [transfer-asset.md](file:///c:/CryptoMarketProject/backend/docs/flows/transfer-asset.md)
- [dashboard-read.md](file:///c:/CryptoMarketProject/backend/docs/flows/dashboard-read.md)
