# 0006 - Redis instead of in-memory caching

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

Several services (`Market.API`, `Portfolio.API`, `Notifications.API`) require fast read access to volatile or high-frequency state, such as real-time coin prices, active price alerts, limit orders during price checks, and financial idempotency tokens. In-memory caching (`IMemoryCache`) versus an external distributed cache (`Redis`) had to be evaluated.

## Decision

Deploy and utilize a centralized **Redis** instance (`redis:6.2-alpine`) as the primary distributed caching and transient state store.

## Alternatives considered

- **In-Memory Caching (`IMemoryCache`):** Retaining process-local in-memory state. Rejected for core caching because data vanishes whenever a container or application restarts. (Note: `Identity.API` still uses `IMemoryCache` exclusively for rate limiting).

## Consequences

- (+) **Survivability Across Service Restarts:** Cached data persists independent of application container lifecycles. If `Market.API` or `Portfolio.API` restarts, cached price snapshots and idempotency tokens remain intact.
- (+) **Compensating for Relational Read Latency:** After migrating `MarketDb` from MongoDB to PostgreSQL (ADR 0002), Redis provides sub-millisecond retrieval of active coin quotes (`market:coins`).
- (+) **Concurrency & Locking:** Provides atomic string operations for idempotency key validation (`Idemp_{key}`) and correlation tracking (`{correlationId}`).
- (-) **Intended Cross-Service Sharing Unexercised:** An intended architectural benefit of Redis was serving as a shared cache between services. However, as verified in [docs/database/redis-keys.md](file:///c:/CryptoMarketProject/backend/docs/database/redis-keys.md), **no Redis keys are currently shared across services**. Each service reads and writes exclusively to its own key namespaces.
- (-) **Persistence Limitations:** Standard Docker Compose configuration mounts `redis_data:/data` using default RDB snapshots. Append-only file (AOF) is not explicitly enabled; therefore, un-snapshotted data could be lost in the event of an abrupt Redis container crash.

## Evidence

- **Docker Compose:** [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L123-L132) deploys `redis:6.2-alpine` on port `6379:6379` with volume `redis_data:/data`.
- **Key Namespace Isolation:** As cataloged in [docs/database/redis-keys.md](file:///c:/CryptoMarketProject/backend/docs/database/redis-keys.md):
  - `Market.API`: `market:coins`, `coins:supply`
  - `Portfolio.API`: `{correlationId}`, `{symbol}Orders`, `{symbol}:Orders`, `Idemp_{idempotencyKey}`
  - `Notifications.API`: `{symbol}Alerts`
  - Zero cross-service read/write overlap exists.

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L67-L76) — Section 0006.

---

## Sources

- [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L123-L132)
- [docs/database/redis-keys.md](file:///c:/CryptoMarketProject/backend/docs/database/redis-keys.md)
- [Market.API/Infrastructure/Caching/RedisCacheService.cs](file:///c:/CryptoMarketProject/backend/Market.API/Infrastructure/Caching/RedisCacheService.cs)
- [Portfolio.API/Application/Services/LimitOrderService.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Services/LimitOrderService.cs)
