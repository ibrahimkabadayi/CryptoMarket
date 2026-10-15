# System Inventory

> **Temporary working file.** Populated in Phase 1 and maintained across phases; archived or deleted in Phase 10.  
> Last updated from codebase at commit `b5229aa`.

---

## 1. Services

| Service Name | Project Path | Port (Host:Container) | Purpose | Dependencies & Backing Services | Key Configuration Keys |
|---|---|---|---|---|---|
| **API.Gateway** | `API.Gateway/API.Gateway.csproj` | `5000:8080` | Reverse proxy and API gateway using YARP; routes public HTTP and SignalR WebSocket requests to internal microservices | `Yarp.ReverseProxy`, `Shared.Observability`, Jaeger (`OTEL_EXPORTER_OTLP_ENDPOINT`) | `ReverseProxy:Routes`, `ReverseProxy:Clusters` |
| **Identity.API** | `Identity.API/Identity.API.csproj` | `8001:8080` | User registration, authentication, JWT token issuance, and user profile queries | PostgreSQL (`IdentityDb`), RabbitMQ, `IMemoryCache`, `Shared.Messages`, `Shared.Observability`, Jaeger | `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `RabbitMQ:Host`, `RabbitMQ:Username`, `RabbitMQ:Password`, `RateLimitSettings:MaxRequests`, `RateLimitSettings:TimeWindowInSeconds` |
| **Market.API** | `Market.API/Market.API.csproj` | `8002:8080` | Cryptocurrency market data, simulated price and supply dynamics, price history candlesticks, Gemini AI market news, and SignalR market price broadcasting | PostgreSQL (`MarketDb`), Redis (`redis_db:6379`), RabbitMQ, Google Gemini REST API, SignalR (`MarketHub`), `Shared.Messages`, `Shared.Observability`, Jaeger | `ConnectionStrings:DefaultConnection`, `Redis:ConnectionString`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Gemini:ApiKey`, `RabbitMQ:Host`, `RabbitMQ:Username`, `RabbitMQ:Password` |
| **Notifications.API** | `Notifications.API/Notifications.API.csproj` | `6000:8080` | In-app notification management, user price alerts evaluation, welcome emails via SMTP, and SignalR live alert/notification dispatching | PostgreSQL (`NotificationsDb`), Redis (`redis_db:6379`), RabbitMQ, SMTP Server, SignalR (`NotificationHub`), `Shared.Messages`, `Shared.Observability`, Jaeger | `ConnectionStrings:DefaultConnection`, `Redis:ConnectionString`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `EmailSettings:Email`, `EmailSettings:Password`, `EmailSettings:Host`, `EmailSettings:Port`, `RabbitMQ:Host`, `RabbitMQ:Username`, `RabbitMQ:Password` |
| **Portfolio.API** | `Portfolio.API/Portfolio.API.csproj` | `7000:8080` | User wallets, fiat balance deposit/withdraw, crypto asset buy/sell/transfers, limit order execution against live prices, treasury fee collection, and SignalR portfolio updates | PostgreSQL (`PortfolioDb`), Redis (`redis_db:6379`), RabbitMQ, SignalR (`PortfolioHub`), `Shared.Messages`, `Shared.Observability`, Jaeger | `ConnectionStrings:DefaultConnection`, `Redis:ConnectionString`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `RabbitMQ:Host`, `RabbitMQ:Username`, `RabbitMQ:Password` |

### Supporting Infrastructure Services (docker-compose.yml)

| Service | Image | Container Name | Ports | Volumes | Role |
|---|---|---|---|---|---|
| **postgres.db** | `postgres:latest` | `postgres_db` | `5432:5432` | `postgres_data:/var/lib/postgresql` | Shared PostgreSQL server running 4 distinct logical databases: `IdentityDb`, `MarketDb`, `NotificationsDb`, `PortfolioDb` |
| **redis** | `redis:6.2-alpine` | `redis_db` | `6379:6379` | `redis_data:/data` | In-memory key-value cache and distributed lock/idempotency store |
| **rabbitmq** | `rabbitmq:3-management` | `rabbitmq` | `5672:5672`, `15672:15672` | `rabbitmq_data:/var/lib/rabbitmq` | Message broker for asynchronous event bus with Web UI on port 15672 |
| **jaeger** | `jaegertracing/all-in-one:latest` | `jaeger` | `16686:16686`, `4317:4317` | None | Distributed tracing collector (OTLP gRPC 4317) and query dashboard (16686) |

*(Note: `docker-compose.yml` also declares a `mongo_data` volume remaining from the previous MongoDB setup before migration).*

---

## 2. Databases per Service & Migration Setup

Every persistent service connects to its own dedicated PostgreSQL database via Entity Framework Core using Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`).

| Database | Owning Service | DbContext Class | Migration Location | Auto-Migrate at Startup? | Tables Managed |
|---|---|---|---|---|---|
| **IdentityDb** | `Identity.API` | `ApplicationDbContext : IdentityDbContext<AppUser, AppRole, Guid>` | `Identity.API/Migrations/` | No (managed out-of-band via `dotnet ef`) | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `__EFMigrationsHistory` |
| **MarketDb** | `Market.API` | `ApplicationDbContext : DbContext` | `Market.API/Migrations/` | No (managed out-of-band via `dotnet ef`) | `Coins`, `MarketNews`, `PriceHistories`, `__EFMigrationsHistory` |
| **NotificationsDb** | `Notifications.API` | `ApplicationDbContext : DbContext` | `Notifications.API/Migrations/` | No (managed out-of-band via `dotnet ef`) | `Notifications`, `PriceAlerts`, `__EFMigrationsHistory` |
| **PortfolioDb** | `Portfolio.API` | `ApplicationDbContext : DbContext` | `Portfolio.API/Migrations/` | No (managed out-of-band via `dotnet ef`) | `Wallets`, `Assets`, `Transactions`, `LimitOrders`, `TreasuryBalances`, `__EFMigrationsHistory` |

### Cross-Service Logical References (No Physical FK Constraints)

Because each service owns an isolated PostgreSQL database, foreign keys across databases cannot exist. The following columns act as logical foreign identifiers:

| Source (service.table.column) | Target (service.entity) | Type | Consistency Enforcement | If Target Deleted |
|---|---|---|---|---|
| `PortfolioDb.Wallets.UserId` | `IdentityDb.AspNetUsers.Id` | `uuid` | Asynchronous creation via `UserCreatedEvent` published upon user registration | Orphaned wallet (no cascade delete configured) |
| `PortfolioDb.LimitOrders.UserId` | `IdentityDb.AspNetUsers.Id` | `uuid` | Populated from request body in direct POST, or from JWT claims in Market flow | Orphaned limit order |
| `NotificationsDb.Notifications.UserId` | `IdentityDb.AspNetUsers.Id` | `uuid` | Populated from event payload (`UserId` / `SourceWalletUserId`) or JWT claims | Orphaned notification |
| `NotificationsDb.PriceAlerts.UserId` | `IdentityDb.AspNetUsers.Id` | `uuid` | Populated from JWT claims in `PriceAlertController` | Orphaned price alert |
| `PortfolioDb.Assets.Symbol` | `MarketDb.Coins.Symbol` | `varchar(50)` | Validated at trade execution time against market prices | Orphaned asset symbol |
| `PortfolioDb.LimitOrders.Symbol` | `MarketDb.Coins.Symbol` | `text` | Matched against price simulation updates | Order never matches if symbol does not exist |
| `PortfolioDb.Transactions.Symbol` | `MarketDb.Coins.Symbol` | `varchar(50)` | Recorded from executed trade symbol | Historical record retained |
| `PortfolioDb.TreasuryBalances.AssetSymbol` | `MarketDb.Coins.Symbol` | `varchar(10)` | Fee deducted in coin asset currency | Accumulated platform fee retained |
| `NotificationsDb.PriceAlerts.Symbol` | `MarketDb.Coins.Symbol` | `text` | Evaluated against `CoinPriceEvent.Symbol` from Market.API | Alert never triggers if symbol does not exist |
| `MarketDb.PriceHistories.Symbol` | `MarketDb.Coins.Symbol` | `varchar(10)` | Populated from generated coin prices (no physical FK inside MarketDb) | Orphaned history records if coin deleted |

### Database Constraints & Notable Schema Details

- **IdentityDb:**
  - `AspNetUsers`: Unique index on `NormalizedUserName` (`UserNameIndex`); unique index on `NormalizedEmail` (`EmailIndex`).
  - `AspNetRoles`: Unique index on `NormalizedName` (`RoleNameIndex`).
- **MarketDb:**
  - `Coins`: Unique index on `Symbol` (`CoinConfiguration.cs`).
  - `PriceHistories`: Composite unique index on `(Symbol, Timestamp)` (`PriceHistoryConfiguration.cs`).
  - `MarketNews`: Index on `PublishedAt`. `RelatedSymbols` is stored as a comma-separated string converted via EF Core value conversion.
- **NotificationsDb:**
  - `Notifications`: Primary key `Id`. No unique indexes configured.
  - `PriceAlerts`: Primary key `Id`. No unique indexes configured.
- **PortfolioDb:**
  - `Wallets`: Primary key `Id`. Default `FiatBalance = 0`. Notice: `UserId` does **not** have a unique database index or unique constraint; wallet uniqueness per user relies on application logic.
  - `Assets`: Primary key `Id`. Foreign key `WalletId` -> `Wallets.Id` with `DeleteBehavior.Restrict`.
  - `Transactions`: Primary key `Id`. Foreign key `WalletId` -> `Wallets.Id` with `DeleteBehavior.Restrict`.
  - `LimitOrders`: Primary key `Id`. Foreign key `WalletId` -> `Wallets.Id` with `DeleteBehavior.NoAction`.
  - `TreasuryBalances`: Primary key `Id`. `AssetSymbol` max length 10. `TotalAmount` decimal(18,8).

---

## 3. Redis Key Catalog & Caching

Redis is used by `Market.API`, `Portfolio.API`, and `Notifications.API`. `Identity.API` does not connect to Redis; it uses in-memory caching (`IMemoryCache`) for rate limiting.

**Cross-Service Sharing Verification:**  
No Redis keys are shared across services. Every service only reads and writes its own specific key namespaces.

| Service | Key Pattern | Value Type | TTL | Purpose | Writer | Reader |
|---|---|---|---|---|---|---|
| **Market.API** | `market:coins` | `List<Coin>` (JSON) | None (indefinite in simulation) / 5s in CoinService fallback | Current live snapshot of all coins and latest prices | `PriceSimulation` (background), `CoinService.GetAllCoins` | `CoinService`, `DatabasePriceUpdate`, `PriceHistoryGenerator`, `PriceSimulation` |
| **Market.API** | `coins:supply` | `List<CoinSupplyDto>` (JSON) | None (updated in loop) | Current circulating supply data for coins | `SupplySimulation` (background) | `SupplySimulation`, `DatabaseSupplyUpdate` |
| **Portfolio.API** | `{correlationId}` (string GUID) | `string` (CorrelationId) | 24 hours | Duplicate-message guard preventing duplicate limit order creation | `LimitOrderService.CreateLimitOrderAsync` | `LimitOrderService.CreateLimitOrderAsync` |
| **Portfolio.API** | `{symbol}Orders` | `List<LimitOrder>` (JSON) | 5 seconds (invalidated on order create) | Short-lived cache of limit orders by symbol | `LimitOrderService.GetLimitOrdersBySymbol` (invalidated in `CreateLimitOrderAsync`) | `LimitOrderService.GetLimitOrdersBySymbol` |
| **Portfolio.API** | `{symbol}:Orders` | `List<LimitOrderCacheDto>` (JSON) | 2 minutes | Cache of active orders evaluated during price matching | `LimitOrderService.RefreshCacheAsync` | `LimitOrderService.GetOrdersAsync` |
| **Portfolio.API** | `Idemp_{idempotencyKey}` | `string` (IdempotencyKey) | 24 hours | HTTP request idempotency check for financial endpoints (POST/PUT) | `IdempotencyMiddleware` | `IdempotencyMiddleware` |
| **Notifications.API** | `{symbol}Alerts` | `List<PriceAlert>` (JSON) | 5 seconds (invalidated on alert create/deactivate) | Short-lived cache of active price alerts for rapid price matching | `PriceAlertService.CheckPriceAlertsAsync` (invalidated in `AddPriceAlertAsync`, `DeactivatePriceAlertAsync`) | `PriceAlertService.GetActiveAlertsBySymbolAsync` |
| **Identity.API** *(In-Memory)* | `RateLimit_{ipAddress}` | `int` (request counter) | Configured `TimeWindowInSeconds` (sliding/absolute) | In-memory IP request throttling | `RateLimitingMiddleware` | `RateLimitingMiddleware` |

### Redis Persistence Setup (docker-compose.yml)

- Image: `redis:6.2-alpine`
- Volume: `redis_data:/data`
- Settings: Default Alpine Redis configuration with volume persistence. Append-only file (AOF) is not explicitly enabled in docker-compose command flags, meaning standard RDB snapshotting applies.

---

## 4. RabbitMQ via MassTransit Messaging

All asynchronous inter-service communication is handled via MassTransit over RabbitMQ.

### Message Types Catalog (`Shared.Messages`)

| Message Contract | Type | Published By (Service & Method) | Consumed By (Service & Queue) | Duplicate Check (CorrelationId in Redis)? | Failure & Retry Behavior |
|---|---|---|---|---|---|
| `UserCreatedEvent` | `record` | `Identity.API` (`UserService.AddUserAsync`) | 1. `Notifications.API` (`UserCreatedConsumer` on `notification-user-created-queue`)<br>2. `Portfolio.API` (`UserCreatedConsumer` on `portfolio-user-created-queue`) | **No** (both consumers lack check) | Default MassTransit retry; faults route to `<queue>_error` |
| `CoinPriceEvent` | `record` | `Market.API` (`PriceSimulation.ExecuteAsync`) | 1. `Notifications.API` (`CoinPriceConsumer` on `notification-coin-price-queue`)<br>2. `Portfolio.API` (`CoinPriceConsumer` on `portfolio-coin-price-queue`) | **No** (both consumers lack check) | Default MassTransit retry; faults route to `<queue>_error` |
| `LimitOrderPlacedEvent` | `record` | `Market.API` (`LimitOrderService.SetLimitOrder`) | `Portfolio.API` (`SetLimitOrderEvent` on `portfolio-set-limit-order-queue`) | **Yes** (`LimitOrderService.CreateLimitOrderAsync` checks Redis `{correlationId}`, TTL 24h) | Rejects duplicate with `InvalidOperationException`; faults route to `portfolio-set-limit-order-queue_error` |
| `LimitOrderOccuredEvent` | `record` | `Portfolio.API` (`LimitOrderService.ApplyLimitOrder`) | `Notifications.API` (`LimitOrderConsumer` on `notification-limit-order-queue`) | **No** | Default MassTransit retry; faults route to `notification-limit-order-queue_error` |
| `AssetTransferEvent` | `record` | `Portfolio.API` (`WalletService.TransferAsset`) | `Notifications.API` (`AssetTransferConsumer` on `notification-asset-transferred-queue`) | **No** | Default MassTransit retry; faults route to `notification-asset-transferred-queue_error` |
| `BuyCoinEvent` | `record` | `Market.API` (`CoinService.BuyCoin`) | `Portfolio.API` (`BuyCoinConsumer` class exists, but **NOT registered in Program.cs**) | **No** | **Unrouted / Dropped:** Consumer class exists in code but is not wired in MassTransit registration |
| `FeeCollectionEvent` | `record` | `Portfolio.API` (`WalletService.BuyAsset`, `SellAsset`) | `Portfolio.API` (`FeeCollectionConsumer` class exists, but **NOT registered in Program.cs**) | **No** | **Unrouted / Dropped:** Consumer class exists in code but is not wired in MassTransit registration |
| `AssetDeposited` | `record` | `Portfolio.API` (`WalletService.DepositMoney`) | **None** (no consumer implemented) | N/A | No consumer subscribed |
| `AssetWithdrawn` | `record` | `Portfolio.API` (`WalletService.WithdrawMoney`) | **None** (no consumer implemented) | N/A | No consumer subscribed |
| `TradeExecuted` | `record` | `Portfolio.API` (`WalletService.BuyAsset`, `SellAsset`) | **None** (no consumer implemented) | N/A | No consumer subscribed |
| `UserAlreadyRegistiredEvent` | `record` | **None** (never published) | **None** (no consumer implemented) | N/A | Unused message contract |

### MassTransit Configuration Details

- **Queue Topology:**
  - Receive endpoints are configured explicitly per consumer using kebab-case names (e.g., `notification-user-created-queue`, `portfolio-coin-price-queue`).
  - Exchanges are created automatically by MassTransit using the full .NET namespace type names (e.g., `Shared.Messages:UserCreatedEvent`).
- **Retry Policies:** None configured in `Program.cs`. Default MassTransit immediate execution applies.
- **Outbox Pattern:** Transactional outbox (`AddEntityFrameworkOutbox`) is **not configured** in any service. Messages are published directly to RabbitMQ during request processing.

---

## 5. HTTP Endpoints per Controller

### Endpoint Security Classification (ADR 0007 Audit)

Each endpoint is classified into one of three categories:
- **(a) JWT-Derived:** User identity is extracted directly from validated JWT claims (`User.FindFirstValue(ClaimTypes.NameIdentifier)`).
- **(b) Anonymous / Non-User:** Public endpoints or admin/resource-specific operations that do not act on behalf of a specific user.
- **(c) User-Scoped but NOT JWT-Derived:** The operation affects user data, but identity is accepted from route or body parameters.

| Controller | Method | Route | Auth Requirement | Request DTO | Response DTO / Type | Classification | Notes / Identity Source |
|---|---|---|---|---|---|---|---|
| **Identity.API** | | | | | | | |
| `AuthController` | `POST` | `/api/auth/register` | Anonymous | `RegisterUserRequest` | `{ Token, Message }` | **(b)** | Creates new user; publishes `UserCreatedEvent` |
| `AuthController` | `POST` | `/api/auth/login` | Anonymous | `LoginRequest` | `{ Token }` | **(b)** | Issues JWT token |
| `AuthController` | `GET` | `/api/auth/{userId:guid}` | Anonymous | None | `UserDto` | **(c)** | **User-scoped, but takes `userId` from route parameter without authentication** |
| **Market.API** | | | | | | | |
| `MarketController` | `POST` | `/api/market/{symbol}` | `[Authorize]` | `BuyCoinRequest` | `200 OK` | **(a)** | Takes `userId` from JWT claims; publishes `BuyCoinEvent` |
| `MarketController` | `POST` | `/api/market/limit-order/{symbol}` | `[Authorize]` | `SetLimitOrderRequest` | `200 OK` | **(a)** | Takes `userId` from JWT claims; publishes `LimitOrderPlacedEvent` |
| `MarketController` | `GET` | `/api/market` | Anonymous | None | `List<Coin>` | **(b)** | Public market listing |
| `MarketController` | `GET` | `/api/market/{symbol}` | Anonymous | None | `Coin` | **(b)** | Public coin details |
| `MarketController` | `GET` | `/api/market/{symbol}/history` | Anonymous | Query: `intervalMinutes`, `hoursBack` | `List<PriceHistoryDto>` | **(b)** | Public candlestick/price history |
| `MarketNewsController` | `GET` | `/api/market-news` | Anonymous | Query: `count` | `List<MarketNewsDto>` | **(b)** | Public market news feed |
| `MarketNewsController` | `GET` | `/api/market-news/{id}` | Anonymous | None | `MarketNewsDto` | **(b)** | Public news by ID |
| `MarketNewsController` | `GET` | `/api/market-news/coin/{symbol}` | Anonymous | None | `List<MarketNewsDto>` | **(b)** | Public news by coin symbol |
| `MarketNewsController` | `DELETE` | `/api/market-news/{id}` | `[Authorize]` | None | `{ message }` | **(b)** | Deletes news article; requires auth but not user-scoped |
| **Notifications.API** | | | | | | | |
| `NotificationController`| `GET` | `/api/notifications` | `[Authorize]` | None | `List<NotificationDto>` | **(a)** | `userId` from JWT claims |
| `NotificationController`| `GET` | `/api/notifications/unread-count` | `[Authorize]` | None | `int` | **(a)** | `userId` from JWT claims |
| `NotificationController`| `PUT` | `/api/notifications/{notificationId}/read` | `[Authorize]` | None | `204 NoContent` | **(a)** | `userId` from JWT claims |
| `NotificationController`| `PUT` | `/api/notifications/mark-all-read` | `[Authorize]` | None | `204 NoContent` | **(a)** | `userId` from JWT claims |
| `NotificationController`| `POST` | `/api/notifications` | `[Authorize]` | `CreateNotificationRequest` | `{ Message }` | **(a)** | `userId` from JWT claims |
| `PriceAlertController` | `POST` | `/api/price-alerts` | `[Authorize]` | `CreatePriceAlertRequest` | `{ Message }` | **(a)** | `userId` from JWT claims |
| `PriceAlertController` | `GET` | `/api/price-alerts/active` | `[Authorize]` | None | `List<PriceAlertDto>` | **(a)** | `userId` from JWT claims |
| `PriceAlertController` | `GET` | `/api/price-alerts/all` | `[Authorize]` | None | `List<PriceAlertDto>` | **(a)** | `userId` from JWT claims |
| `PriceAlertController` | `PUT` | `/api/price-alerts/{priceAlertId}/deactivate` | `[Authorize]` | None | `204 NoContent` | **(a)** | `userId` from JWT claims |
| **Portfolio.API** | | | | | | | |
| `WalletController` | `GET` | `/api/wallet` | `[Authorize]` | None | `{ result: PortfolioDashboardDto }` | **(a)** | `userId` from JWT claims |
| `WalletController` | `POST` | `/api/wallet/withdrawals` | `[Authorize]` | `WithdrawMoneyRequest` | `200 OK` | **(a)** | `userId` from JWT claims |
| `WalletController` | `POST` | `/api/wallet/deposits` | `[Authorize]` | `DepositMoneyRequest` | `{ Message }` | **(a)** | `userId` from JWT claims |
| `WalletController` | `POST` | `/api/wallet/assets/{symbol}/purchases` | `[Authorize]` | `BuyAssetRequest` | `200 OK` (string) | **(a)** | `userId` from JWT claims |
| `WalletController` | `POST` | `/api/wallet/assets/{symbol}/transfers` | `[Authorize]` | `TransferAssetRequest` | `{ Message }` | **(a)** | `userId` from JWT claims |
| `WalletController` | `POST` | `/api/wallet/assets/{symbol}/sales` | `[Authorize]` | `SellAssetRequest` | `200 OK` | **(a)** | `userId` from JWT claims |
| `LimitOrderController` | `POST` | `/api/limit-orders` | `[Authorize]` | `CreateLimitOrderRequest`, Header: `Idempotency-Key` | `200 OK` | **(c)** | **User-scoped, but takes `UserId` from `request.UserId` (body), not from JWT** |
| `LimitOrderController` | `GET` | `/api/limit-orders/{id}` | `[Authorize]` | None | `LimitOrderDto` | **(b)** | Retrieves order by ID; does not check `UserId` ownership |
| `LimitOrderController` | `GET` | `/api/limit-orders` | `[Authorize]` | None | `List<LimitOrderDto>` | **(b)** | Returns all orders in system; does not filter by user |
| `LimitOrderController` | `PATCH` | `/api/limit-orders/{id}` | `[Authorize]` | `UpdateLimitOrderRequest` | `200 OK` (string) | **(b)** | Updates order by ID; does not check `UserId` ownership |
| `LimitOrderController` | `DELETE` | `/api/limit-orders/{id}` | `[Authorize]` | None | `200 OK` | **(b)** | Deletes order by ID; does not check `UserId` ownership |

### Gateway Route Discrepancy Note

In `API.Gateway/appsettings.json`, the route for portfolio wallets is configured as:
- Path: `"/api/wallets/{**catch-all}"`
However, `WalletController.cs` in `Portfolio.API` defines its route attribute as:
- `[Route("api/wallet")]` (singular)

---

## 6. Service-to-Service HTTP Calls

- **Direct inter-service HTTP calls:** **None (0)**.  
  No microservice invokes another microservice via HTTP. All inter-service collaboration occurs through RabbitMQ event messaging.
- **External HTTP calls:**
  - `Market.API` calls the external Google Gemini API endpoint (`https://generativelanguage.googleapis.com/...`) via `GeminiService` using typed `HttpClient` to generate synthetic market news articles.

---

## 7. Middleware Pipelines per Service

The execution order of middleware registered in `Program.cs` is critical for security, error handling, and observability.

### Registration Order by Service

#### 1. Identity.API
1. `UseCors()` — Applies default CORS policy allowing localhost.
2. `UseCorrelationIdMiddleware()` — Reads or generates `X-Correlation-ID` header; populates `HttpContext.Items` and response headers.
3. `UseLoggingMiddleware()` — Starts stopwatch, logs HTTP method, path, status code, and duration in ms.
4. `UseExceptionHandlingMiddleware()` — Global try/catch handler; formats unhandled exceptions as standard JSON with status 500.
5. `UseRateLimitingMiddleware()` — Enforces IP-based request rate limits via in-memory cache; returns 429 if threshold exceeded.
6. `UseAuthorization()` — Evaluates authorization policies (if endpoints specify).
7. `MapControllers()` — Dispatches to controllers.
8. `MapCustomHealthChecks()` — Maps health check endpoints.

#### 2. Market.API
1. `UseCors("AllowVueApp")` — Allows requests and credentials from `http://localhost:5173`.
2. `UseCorrelationIdMiddleware()` — Reads/generates `X-Correlation-ID`.
3. `UseLoggingMiddleware()` — Measures request execution time.
4. `UseExceptionHandlingMiddleware()` — Catches exceptions, writes 500 error response.
5. `UseAuthentication()` — Validates JWT Bearer tokens.
6. `UseAuthorization()` — Enforces `[Authorize]` attributes.
7. `MapControllers()` — Dispatches to controllers.
8. `MapHub<MarketHub>("hubs/market")` — Maps SignalR hub for live market price feeds.
9. `MapCustomHealthChecks()` — Maps health check endpoints.

#### 3. Notifications.API
1. `UseCorrelationIdMiddleware()` — Reads/generates `X-Correlation-ID`.
2. `UseLoggingMiddleware()` — Request duration logging.
3. `UseExceptionHandlingMiddleware()` — Catches unhandled exceptions.
4. `UseAuthentication()` — Validates JWT Bearer tokens (with query string token extraction for SignalR).
5. `UseAuthorization()` — Enforces `[Authorize]`.
6. `UseCors("SignalRPolicy")` — Allows origins `http://localhost:3000` and `http://localhost:5173`.
7. `MapControllers()` — Dispatches to controllers.
8. `MapHub<NotificationHub>("hubs/notifications")` — Maps SignalR hub for live alerts and notifications.
9. `MapCustomHealthChecks()` — Maps health check endpoints.

#### 4. Portfolio.API
1. `UseCors("AllowVueApp")` — Allows origins from `http://localhost:5173`.
2. `UseCorrelationIdMiddleware()` — Reads/generates `X-Correlation-ID`.
3. `UseLoggingMiddleware()` — Request duration logging.
4. `UseExceptionHandlingMiddleware()` — Catches unhandled exceptions.
5. `UseAuthentication()` — Validates JWT Bearer tokens (with query string token extraction for SignalR).
6. `UseAuthorization()` — Enforces `[Authorize]`.
7. `Inline Request Logger Delegate` — Buffers request body, hashes content (SHA-256), and logs incoming `Idempotency-Key` and `X-Request-ID`.
8. `UseIdempotencyMiddleware()` — Requires `Idempotency-Key` header on POST/PUT/DELETE requests; validates against Redis key `Idemp_{key}`; rejects duplicate/concurrent requests with 409 Conflict.
9. `MapControllers()` — Dispatches to controllers.
10. `MapHub<PortfolioHub>("hubs/portfolio")` — Maps SignalR hub for live wallet updates.
11. `MapCustomHealthChecks()` — Maps health check endpoints.

#### 5. API.Gateway
1. `UseCors()` — Applies default localhost CORS policy.
2. `MapReverseProxy()` — Executes YARP reverse proxy pipeline based on routing rules.

---

## 8. Stateful Domain Entities & State Transitions

### `LimitOrder` (Portfolio.API)

The only entity in the system governed by an explicit multi-state lifecycle machine:

```
                  ┌──────────────────────┐
                  │       Pending        │ ◄── Initial State on Order Creation
                  └──────────┬───────────┘
                             │
                             │ Matched by price in CheckLimitOrders
                             ▼
                  ┌──────────────────────┐
                  │      Processing      │
                  └──────────┬───────────┘
                             │
                             │ Executed in ApplyLimitOrder
                             ▼
                  ┌──────────────────────┐
                  │        Filled        │
                  └──────────────────────┘

       [ Canceled ]  ── Enum value exists (4), but code deletes the record rather than setting status
```

- **States (`LimitOrderStatus` enum):**
  - `Pending = 1`: Default status set when a limit order is created (`LimitOrder.OrderStatus = LimitOrderStatus.Pending`).
  - `Processing = 2`: Set temporarily when `CheckLimitOrders` identifies that current market price meets the trigger condition (`IsTriggered`).
  - `Filled = 3`: Set when `ApplyLimitOrder` successfully buys/sells the asset in the wallet and publishes `LimitOrderOccuredEvent`.
  - `Canceled = 4`: Defined in `LimitOrderStatus.cs`. In active code, canceling an order calls `limitOrderRepository.DeleteAsync(limitOrderId)` (physical row deletion), so rows in state `Canceled` are not persisted.

### Other Entities with State Flags

- **`Notification` (Notifications.API):**
  - `IsRead` (boolean, default `false`).
  - Transitions to `true` with `ReadAt = DateTime.UtcNow` via `Notification.MarkAsRead()`.
- **`PriceAlert` (Notifications.API):**
  - `IsActive` (boolean, default `true`).
  - Transitions to `false` via `PriceAlert.Deactivate()`.

---

## 9. Git History Evidence: MarketDb MongoDB → PostgreSQL Migration

Evidence for ADR 0002 documenting the transition of `Market.API` from MongoDB to PostgreSQL:

- **Date:** 2026-07-10 (between 11:46:59 and 11:47:28 +03:00)
- **Author:** `ibrahimkabadayi <ibrahimkabadayi180@gmail.com>`
- **Commits:**
  1. `4e73f7aa5735fe983af55e80245dddc6b0d6a6b2` (11:46:59):  
     *Message:* `Remove MarketDbContext and MongoDbSettings classes from infrastructure context`  
     *Changes:* Deleted `Market.API/Infrastructure/Context/MarketContext.cs` and `MongoDbSettings.cs`.
  2. `a2743c575f0bfa774924cb8e3107b7dd5edc348e` (11:47:06):  
     *Message:* `Refactor entities to replace MongoDB-specific attributes with standard .NET types`  
     *Changes:* Removed MongoDB attributes (`[BsonId]`, `[BsonRepresentation]`, `ObjectId`) from `Market.API/Domain/Entities/BaseEntity.cs` and `Coin.cs`.
  3. `64159d5bbdab50d8520fa8f61e48ab6ab83a75d8` (11:47:13):  
     *Message:* `Refactor CoinRepository to use Entity Framework; replace MongoDB-specific operations with EF methods`  
     *Changes:* Converted `Market.API/Infrastructure/Repositories/CoinRepository.cs` from Mongo collections to EF Core DbContext.
  4. `ee37e998b185116cabbc0f57db92b6e1eee90115` (11:47:20):  
     *Message:* `Migrate infrastructure from MongoDB to Entity Framework; replace MongoDB client with DbContext and update service registrations`  
     *Changes:* Replaced MongoDB client registrations with `services.AddDbContext<ApplicationDbContext>` in `Market.API/Infrastructure/DependencyInjection.cs`.
  5. `f92b99241605d1665d6dabf118847149228b81e9` (11:47:28):  
     *Message:* `Migrate from MongoDB to PostgreSQL; update connection strings, remove MongoDB service from docker-compose, and reconfigure dependencies`  
     *Changes:* Replaced `MongoDbSettings` with `ConnectionStrings:DefaultConnection` in `Market.API/appsettings.json`, removed `mongodb` container service from `docker-compose.yml`, added `postgres.db` dependency.
- **Artifact Residuals:**
  - `docker-compose.yml` still declares `mongo_data:` in its `volumes:` block at line 159.

---

## 10. Background Hosted Services Discovery

| Service | Hosted Service Class | Type | Purpose | Frequency / Trigger |
|---|---|---|---|---|
| **Market.API** | `PriceSimulation` | `BackgroundService` | Simulates randomized price movements for all active coins, publishes `CoinPriceEvent` to RabbitMQ, updates Redis `market:coins`, and broadcasts to `MarketHub` | Every 1 second |
| **Market.API** | `DatabasePriceUpdate` | `BackgroundService` | Reads current prices from Redis `market:coins` and flushes batches to PostgreSQL `MarketDb.Coins` | Periodic timer (every 10 seconds) |
| **Market.API** | `SupplySimulation` | `BackgroundService` | Adjusts circulating supply in Redis `coins:supply` | Periodic loop |
| **Market.API** | `DatabaseSupplyUpdate` | `BackgroundService` | Persists circulating supply from Redis `coins:supply` to PostgreSQL `MarketDb.Coins` | Periodic timer |
| **Market.API** | `PriceHistoryGenerator` | `BackgroundService` | Aggregates price snapshots from Redis `market:coins` and inserts historical candlestick rows into PostgreSQL `MarketDb.PriceHistories` | Periodic interval |
| **Market.API** | `MarketNewsGenerator` | `BackgroundService` | Calls Google Gemini API to generate synthetic market news and saves them to PostgreSQL `MarketDb.MarketNews` | Periodic background schedule |

---

## 11. Known Code Inconsistencies & Unresolved Items

1. **Unregistered MassTransit Consumers:**
   - `Portfolio.API/Consumers/BuyCoinConsumer.cs` (`IConsumer<BuyCoinEvent>`) is implemented but not registered in `Portfolio.API/Program.cs`. `BuyCoinEvent` published by `MarketController` / `CoinService` is never consumed.
   - `Portfolio.API/Consumers/FeeCollectionConsumer.cs` (`IConsumer<FeeCollectionEvent>`) is implemented but not registered in `Portfolio.API/Program.cs`. `FeeCollectionEvent` published by `WalletService` is never consumed.
2. **Unconsumed Published Events:**
   - `Portfolio.API/Application/Services/WalletService.cs` publishes `AssetDeposited`, `AssetWithdrawn`, and `TradeExecuted`, but there are no consumers subscribed to these events anywhere in the system.
3. **Gateway Route Mismatch:**
   - `API.Gateway/appsettings.json` matches `/api/wallets/{**catch-all}` for cluster `portfolio-cluster`, while `WalletController.cs` specifies `[Route("api/wallet")]` (singular).
4. **Endpoint Security Anomalies (ADR 0007):**
   - `Identity.API`: `GET /api/auth/{userId:guid}` takes `userId` from the route and does not require authentication.
   - `Portfolio.API`: `POST /api/limit-orders` takes `UserId` from the request body (`request.UserId`), rather than extracting it from JWT claims.
5. **Wallet Uniqueness:**
   - `PortfolioDb.Wallets` table has no unique constraint or index on `UserId`.
6. **LimitOrder Canceled State:**
   - `LimitOrderStatus.Canceled` is defined, but canceling an order physically deletes the row via `DeleteAsync`.

---

## Sources

- `API.Gateway/Program.cs`
- `API.Gateway/appsettings.json`
- `docker-compose.yml`
- `Identity.API/Program.cs`
- `Identity.API/Controllers/AuthController.cs`
- `Identity.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Identity.API/Infrastructure/Configurations/UserConfiguration.cs`
- `Identity.API/Middlewares/RateLimitingMiddleware.cs`
- `Market.API/Program.cs`
- `Market.API/Controllers/MarketController.cs`
- `Market.API/Controllers/MarketNewsController.cs`
- `Market.API/Application/Services/CoinService.cs`
- `Market.API/Application/Services/GeminiService.cs`
- `Market.API/Application/Services/LimitOrderService.cs`
- `Market.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Market.API/Infrastructure/Configurations/CoinConfiguration.cs`
- `Market.API/Infrastructure/Configurations/MarketNewsConfiguration.cs`
- `Market.API/Infrastructure/Configurations/PriceHistoryConfiguration.cs`
- `Market.API/Infrastructure/Caching/RedisCacheService.cs`
- `Market.API/Infrastructure/BackgroundServices/*.cs`
- `Market.API/Hubs/MarketHub.cs`
- `Notifications.API/Program.cs`
- `Notifications.API/Controllers/NotificationController.cs`
- `Notifications.API/Controllers/PriceAlertController.cs`
- `Notifications.API/Application/Services/NotificationService.cs`
- `Notifications.API/Application/Services/PriceAlertService.cs`
- `Notifications.API/Consumers/*.cs`
- `Notifications.API/Domain/Entities/*.cs`
- `Notifications.API/Hubs/NotificationHub.cs`
- `Portfolio.API/Program.cs`
- `Portfolio.API/Controllers/WalletController.cs`
- `Portfolio.API/Controllers/LimitOrderController.cs`
- `Portfolio.API/Application/Services/WalletService.cs`
- `Portfolio.API/Application/Services/LimitOrderService.cs`
- `Portfolio.API/Consumers/*.cs`
- `Portfolio.API/Domain/Entities/*.cs`
- `Portfolio.API/Domain/Enums/*.cs`
- `Portfolio.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Portfolio.API/Infrastructure/Configurations/*.cs`
- `Portfolio.API/Middlewares/IdempotencyMiddleware.cs`
- `Portfolio.API/Hubs/PortfolioHub.cs`
- `Shared.Messages/*.cs`
- `Shared.Observability/Middlewares/*.cs`
- Git commit log history (`f92b992`, `ee37e99`, `64159d5`, `a2743c5`, `4e73f7a`)
- `docs/decisions/_owner-notes.md`
