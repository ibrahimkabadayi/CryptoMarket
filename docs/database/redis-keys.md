# Redis Key Catalog

> Comprehensive catalog of all Redis key patterns, value formats, TTLs, writers, readers, and persistence configurations across the CryptoMarket platform.

---

## Key Catalog

| Key Pattern | Value Type | TTL | Purpose | Writer (Service & Method) | Reader (Service & Method) |
|---|---|---|---|---|---|
| `market:coins` | JSON string (`List<Coin>`) | None (indefinite in simulation) / 5s in CoinService fallback | Current live snapshot of all cryptocurrency coins with latest simulated prices | `Market.API`: `PriceSimulation.ExecuteAsync`, `CoinService.GetAllCoins` | `Market.API`: `CoinService`, `DatabasePriceUpdate`, `PriceHistoryGenerator`, `PriceSimulation` |
| `coins:supply` | JSON string (`List<CoinSupplyDto>`) | None (updated in continuous loop) | Current circulating supply for active coins | `Market.API`: `SupplySimulation.ExecuteAsync` | `Market.API`: `SupplySimulation`, `DatabaseSupplyUpdate` |
| `{symbol}Orders` | JSON string (`List<LimitOrder>`) | 5 seconds (invalidated immediately upon order creation) | Short-lived cache of limit orders by symbol to avoid repeated database scans | `Portfolio.API`: `LimitOrderService.GetLimitOrdersBySymbol` (invalidated in `CreateLimitOrderAsync`) | `Portfolio.API`: `LimitOrderService.GetLimitOrdersBySymbol` |
| `{symbol}:Orders` | JSON string (`List<LimitOrderCacheDto>`) | 2 minutes | Cache of pending orders evaluated by `CheckLimitOrders` against price ticks | `Portfolio.API`: `LimitOrderService.RefreshCacheAsync` | `Portfolio.API`: `LimitOrderService.GetOrdersAsync` |
| `{correlationId}` (string GUID) | `string` (CorrelationId) | 24 hours | Duplicate-message guard preventing duplicate limit order creation | `Portfolio.API`: `LimitOrderService.CreateLimitOrderAsync` | `Portfolio.API`: `LimitOrderService.CreateLimitOrderAsync` |
| `Idemp_{idempotencyKey}` | `string` (IdempotencyKey) | 24 hours | HTTP request idempotency check on state-changing requests (POST/PUT/DELETE) | `Portfolio.API`: `IdempotencyMiddleware.InvokeAsync` | `Portfolio.API`: `IdempotencyMiddleware.InvokeAsync` |
| `{symbol}Alerts` | JSON string (`List<PriceAlert>`) | 5 seconds (invalidated on alert create / deactivate) | Short-lived cache of active price alerts for fast evaluation against incoming `CoinPriceEvent`s | `Notifications.API`: `PriceAlertService.CheckPriceAlertsAsync` (invalidated in `AddPriceAlertAsync`, `DeactivatePriceAlertAsync`) | `Notifications.API`: `PriceAlertService.GetActiveAlertsBySymbolAsync` |

---

## In-Memory Cache Reference (Non-Redis)

| Key Pattern | Cache Provider | Value Type | TTL | Purpose | Writer & Reader |
|---|---|---|---|---|---|
| `RateLimit_{ipAddress}` | `IMemoryCache` (Local memory) | `int` (request counter) | Configured in `RateLimitSettings:TimeWindowInSeconds` (sliding/absolute) | Per-IP request throttling in Identity service | `Identity.API`: `RateLimitingMiddleware` |

*(Note: `Identity.API` does not connect to Redis; rate limiting state is maintained in-process via `IMemoryCache`).*

---

## Cross-Service Sharing Audit

- **Verification:** **No Redis keys are shared across microservices.**
- Every service accesses only its own key patterns:
  - `Market.API` only reads and writes `market:coins` and `coins:supply`.
  - `Portfolio.API` only reads and writes `{symbol}Orders`, `{symbol}:Orders`, `{correlationId}`, and `Idemp_*`.
  - `Notifications.API` only reads and writes `{symbol}Alerts`.
- While Redis deployment is unified into a single container (`redis_db:6379`) in `docker-compose.yml`, each service operates as an isolated tenant.

---

## Redis Persistence Configuration

From `docker-compose.yml`:
- **Image:** `redis:6.2-alpine`
- **Container Name:** `redis_db`
- **Port:** `6379:6379`
- **Volume:** `redis_data:/data`
- **Persistence Mechanism:** Default Redis Alpine snapshotting (`RDB`). Redis periodically creates point-in-time snapshots of the dataset to disk inside the mounted Docker volume (`/data`).
- **Restart Behavior:** Data saved in the last RDB dump survives container restarts as long as the Docker volume `redis_data` is preserved. Changes made since the last snapshot will be lost if Redis crashes before a new snapshot is generated, as Append-Only File (`AOF`) logging is not explicitly enabled.

---

## Sources

- `Market.API/Infrastructure/Caching/RedisCacheService.cs`
- `Market.API/Application/Services/CoinService.cs`
- `Market.API/Infrastructure/BackgroundServices/*.cs`
- `Portfolio.API/Infrastructure/Caching/CacheService.cs`
- `Portfolio.API/Application/Services/LimitOrderService.cs`
- `Portfolio.API/Middlewares/IdempotencyMiddleware.cs`
- `Notifications.API/Infrastructure/Caching/CacheService.cs`
- `Notifications.API/Application/Services/PriceAlertService.cs`
- `Identity.API/Middlewares/RateLimitingMiddleware.cs`
- `docker-compose.yml` — `redis` service and `redis_data` volume
- `docs/_inventory.md` — Section 3 (Redis Key Catalog)
