# Known Limitations & Technical Debt

> Honest inventory of architectural gaps, technical debt, incomplete integrations, and unhandled failure paths in the CryptoMarket system as currently implemented.

---

## 1. Messaging & Asynchronous Event Gaps

### Unregistered MassTransit Consumers
- **`BuyCoinConsumer` (`Portfolio.API/Consumers/BuyCoinConsumer.cs`):**  
  Implements `IConsumer<BuyCoinEvent>`. However, this consumer is **not registered** in `Portfolio.API/Program.cs`. When `MarketController` or `CoinService` in `Market.API` publishes `BuyCoinEvent`, the message is delivered to RabbitMQ exchange `Shared.Messages:BuyCoinEvent` with zero subscribers and is discarded.
- **`FeeCollectionConsumer` (`Portfolio.API/Consumers/FeeCollectionConsumer.cs`):**  
  Implements `IConsumer<FeeCollectionEvent>` to credit platform fees into `TreasuryBalances`. However, this consumer is **not registered** in `Portfolio.API/Program.cs`. Fees published by `WalletService.BuyAsset` and `SellAsset` are never credited to treasury tables.

### Unconsumed Published Domain Events
- **`Portfolio.API/Application/Services/WalletService.cs`** publishes three domain events via MassTransit during wallet operations:
  - `AssetDeposited` (published upon fiat deposit)
  - `AssetWithdrawn` (published upon fiat withdrawal)
  - `TradeExecuted` (published upon asset purchase or sale)  
  **Status:** No microservice or consumer anywhere in the solution subscribes to these three contracts.

### Asymmetric Consumer Deduplication Guards
- **Only One Consumer Guards Against Duplicates:** Only `LimitOrderService.CreateLimitOrderAsync` (handling `LimitOrderPlacedEvent`) checks Redis for existing `CorrelationId` keys before persisting.
- **Unguarded Consumers:** The following consumers lack deduplication checks and will process duplicate RabbitMQ messages if redelivered:
  - `Notifications.API/Consumers/UserCreatedConsumer.cs`
  - `Notifications.API/Consumers/CoinPriceConsumer.cs`
  - `Notifications.API/Consumers/LimitOrderConsumer.cs`
  - `Notifications.API/Consumers/AssetTransferConsumer.cs`
  - `Portfolio.API/Consumers/UserCreatedConsumer.cs`
  - `Portfolio.API/Consumers/CoinPriceConsumer.cs`

### Absence of Transactional Outbox
- No service configures the MassTransit Transactional Outbox pattern (`AddEntityFrameworkOutbox`). Events are published to RabbitMQ directly in HTTP request threads or background timers. If a database transaction commits but the network or broker fails before message dispatch (or vice-versa), inconsistency can occur.

---

## 2. API Gateway & Routing Inconsistencies

### Portfolio Route Discrepancy
- In [API.Gateway/appsettings.json](file:///c:/CryptoMarketProject/backend/API.Gateway/appsettings.json), the reverse proxy route for portfolio operations is defined as:
  ```json
  "Match": { "Path": "/api/wallets/{**catch-all}" }
  ```
- In [Portfolio.API/Controllers/WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs), the route attribute is configured as singular:
  ```csharp
  [Route("api/wallet")]
  ```
- Direct calls to `http://localhost:5000/api/wallets` will fail to match unless the path is rewritten or corrected to singular `/api/wallet`.

---

## 3. Middleware & Security Inconsistencies

### Localized Rate Limiting
- `RateLimitingMiddleware` is registered **exclusively** in `Identity.API`.
- Neither `API.Gateway`, `Market.API`, `Portfolio.API`, nor `Notifications.API` enforce request rate limiting or anti-DDoS throttling.

### Localized Idempotency Middleware
- `IdempotencyMiddleware` is configured **exclusively** in `Portfolio.API`.
- Modifying actions in other services (e.g., creating price alerts in `Notifications.API` or placing orders via `Market.API`) do not enforce `Idempotency-Key` headers.

---

## 4. Domain & Database Limitations

### Limit Order Cancellation (Hard Deletion vs Enum State)
- The enum value `LimitOrderStatus.Canceled = 4` exists in `LimitOrderStatus.cs`.
- However, `LimitOrderController.DeleteLimitOrder` invokes `LimitOrderRepository.DeleteAsync(limitOrderId)`, executing a physical SQL `DELETE FROM "LimitOrders" WHERE "Id" = @id`. Order history and canceled state are therefore never retained in the database.

### Missing Unique Constraint on `Wallets.UserId`
- In `PortfolioDb`, the `Wallets` table has a default index on primary key `Id`, but **does not have a unique constraint or unique index on `UserId`**.
- Ensuring that a user has only one wallet relies entirely on application-level checks and the initial `UserCreatedConsumer`.

### Dangling Docker Compose Volume
- [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L159) line 159 still declares named volume `mongo_data:`, a residual artifact left behind after migrating `MarketDb` to PostgreSQL in commit `f92b992`.

### SignalR Scaling Without Distributed Backplane
- `MarketHub`, `NotificationHub`, and `PortfolioHub` use ASP.NET Core's default in-memory SignalR connection manager. If scaled horizontally to multiple container replicas behind a load balancer, clients connected to replica A will not receive broadcast messages emitted on replica B unless a Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) is added.

---

## Sources

- [docs/_inventory.md](file:///c:/CryptoMarketProject/backend/docs/_inventory.md#L342-L360)
- [Portfolio.API/Consumers/BuyCoinConsumer.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Consumers/BuyCoinConsumer.cs)
- [Portfolio.API/Consumers/FeeCollectionConsumer.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Consumers/FeeCollectionConsumer.cs)
- [API.Gateway/appsettings.json](file:///c:/CryptoMarketProject/backend/API.Gateway/appsettings.json)
- [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L159)
- [Portfolio.API/Controllers/LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs)
