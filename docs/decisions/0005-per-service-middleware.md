# 0005 - Per-service middleware pipelines: purpose and rationale

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

Each microservice exposes HTTP endpoints and SignalR hubs with varying cross-cutting requirements such as distributed request correlation, performance logging, global exception formatting, denial-of-service throttling, and financial idempotency guarantees. The ordering and scope of ASP.NET Core middleware pipelines must ensure these concerns execute predictably.

## Decision

Adopt standard cross-cutting middleware shared across all services via `Shared.Observability`, with targeted specialized middleware in specific services:
1. **Shared Observability Middleware (All Services):**
   - `CorrelationIdMiddleware`: Extracts or creates `X-Correlation-ID` header; propagates correlation context through logging and downstream responses.
   - `LoggingMiddleware`: Measures request execution time using `Stopwatch` and records HTTP method, route, response status, and duration in ms.
   - `ExceptionHandlingMiddleware`: Centralized try/catch wrapper that intercepts unhandled exceptions and writes a uniform JSON 500 error response.
2. **Specialized Middleware:**
   - `RateLimitingMiddleware` (`Identity.API` only): Throttles IP-based request rates using in-memory cache (`RateLimit_{ip}`) to protect authentication endpoints against brute-force attacks.
   - `IdempotencyMiddleware` (`Portfolio.API` only): Intercepts financial mutation requests (`POST`, `PUT`, `DELETE`), enforces presence of `Idempotency-Key` header, caches keys in Redis (`Idemp_{key}`) with a 24-hour TTL, and rejects duplicate or concurrent execution attempts with `409 Conflict`.

## Alternatives considered

- **Uniform Middleware in Gateway Only:** Moving all rate limiting, idempotency, and exception handling into `API.Gateway`. Rejected because individual services need fine-grained control (e.g., Portfolio requires domain-level transaction idempotency keys; Identity requires local IP rate limiting).

## Consequences

- (+) **Unified Telemetry:** Every incoming HTTP request across all services automatically receives correlation IDs and performance logs.
- (+) **Financial Safety:** `Portfolio.API` protects wallet operations (transfers, deposits, withdrawals) against network retries resulting in double-spending or duplicate order placements.
- (-) **Inconsistent Protection:** Rate limiting is **only** implemented in `Identity.API`. Other endpoints (`Market.API`, `Portfolio.API`, `Notifications.API`) have no rate limiting configured.
- (-) **HTTP vs Message Deduplication Divergence:** `IdempotencyMiddleware` operates strictly on HTTP requests via headers. It does not protect against duplicate MassTransit message consumption, which requires separate event-level deduplication.

## Evidence

- **Pipeline Ordering in Code:**
  - `Identity.API/Program.cs`: `UseCors` -> `UseCorrelationIdMiddleware` -> `UseLoggingMiddleware` -> `UseExceptionHandlingMiddleware` -> `UseRateLimitingMiddleware` -> `UseAuthorization` -> `MapControllers`.
  - `Market.API/Program.cs`: `UseCors` -> `UseCorrelationIdMiddleware` -> `UseLoggingMiddleware` -> `UseExceptionHandlingMiddleware` -> `UseAuthentication` -> `UseAuthorization` -> `MapControllers` -> `MapHub`.
  - `Notifications.API/Program.cs`: `UseCorrelationIdMiddleware` -> `UseLoggingMiddleware` -> `UseExceptionHandlingMiddleware` -> `UseAuthentication` -> `UseAuthorization` -> `UseCors` -> `MapControllers` -> `MapHub`.
  - `Portfolio.API/Program.cs`: `UseCors` -> `UseCorrelationIdMiddleware` -> `UseLoggingMiddleware` -> `UseExceptionHandlingMiddleware` -> `UseAuthentication` -> `UseAuthorization` -> inline body hasher logger -> `UseIdempotencyMiddleware` -> `MapControllers` -> `MapHub`.
- Detailed pipeline breakdown documented in [docs/architecture/middleware.md](file:///c:/CryptoMarketProject/backend/docs/architecture/middleware.md).

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L51-L66) — Section 0005.

---

## Sources

- [Identity.API/Middlewares/RateLimitingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Middlewares/RateLimitingMiddleware.cs)
- [Portfolio.API/Middlewares/IdempotencyMiddleware.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Middlewares/IdempotencyMiddleware.cs)
- [Shared.Observability/Middlewares/CorrelationIdMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/CorrelationIdMiddleware.cs)
- [Shared.Observability/Middlewares/LoggingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/LoggingMiddleware.cs)
- [Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs)
- [docs/architecture/middleware.md](file:///c:/CryptoMarketProject/backend/docs/architecture/middleware.md)
