# Middleware Pipelines

> Per-service middleware inventory: execution order as registered in `Program.cs`, individual middleware responsibilities, and architectural rationale.

---

## Architecture Overview

The HTTP request pipeline in each microservice is composed of sequential ASP.NET Core middleware components. These provide cross-cutting capabilities such as request correlation, duration logging, structured exception formatting, security policies, rate limiting, and request idempotency.

For the architectural rationale regarding middleware selection, ordering, and service-specific responsibilities, see [ADR 0005: Per-service middleware pipelines](../decisions/0005-per-service-middleware.md).

---

## Per-Service Pipeline Order

### 1. Identity.API

Execution order in [Identity.API/Program.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Program.cs):

| Order | Middleware | Purpose / Action | Source / Implementation |
|:---:|---|---|---|
| 1 | `UseCors()` | Applies default CORS policy allowing browser client access | Framework |
| 2 | `UseCorrelationIdMiddleware()` | Extracts or generates `X-Correlation-ID` header; attaches to `HttpContext.Items` and response | [CorrelationIdMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/CorrelationIdMiddleware.cs) |
| 3 | `UseLoggingMiddleware()` | Starts stopwatch, logs HTTP method, path, response status, and duration in ms | [LoggingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/LoggingMiddleware.cs) |
| 4 | `UseExceptionHandlingMiddleware()` | Global try/catch handler returning standardized JSON 500 responses | [ExceptionHandlingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs) |
| 5 | `UseRateLimitingMiddleware()` | Throttles requests by IP address using `IMemoryCache`; returns 429 if threshold exceeded | [RateLimitingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Middlewares/RateLimitingMiddleware.cs) |
| 6 | `UseAuthorization()` | Enforces authorization checks where applicable | Framework |
| 7 | `MapControllers()` | Routes HTTP requests to matching controller actions | Framework |
| 8 | `MapCustomHealthChecks()` | Exposes `/health/live` and `/health/ready` endpoints | [HealthCheckExtensions.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/HealthChecks/HealthCheckExtensions.cs) |

> **Note:** Rate limiting is **only** configured in `Identity.API`. No other service in the system implements rate limiting middleware.

---

### 2. Market.API

Execution order in [Market.API/Program.cs](file:///c:/CryptoMarketProject/backend/Market.API/Program.cs):

| Order | Middleware | Purpose / Action | Source / Implementation |
|:---:|---|---|---|
| 1 | `UseCors("AllowVueApp")` | Allows cross-origin requests and credentials from `http://localhost:5173` | Framework |
| 2 | `UseCorrelationIdMiddleware()` | Injects or extracts `X-Correlation-ID` for end-to-end tracing | [CorrelationIdMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/CorrelationIdMiddleware.cs) |
| 3 | `UseLoggingMiddleware()` | Measures request execution duration and logs request status | [LoggingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/LoggingMiddleware.cs) |
| 4 | `UseExceptionHandlingMiddleware()` | Catches unhandled exceptions, writes structured 500 error response | [ExceptionHandlingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs) |
| 5 | `UseAuthentication()` | Validates JWT Bearer tokens | Framework |
| 6 | `UseAuthorization()` | Enforces `[Authorize]` attributes on authenticated actions | Framework |
| 7 | `MapControllers()` | Dispatches to API controllers (`MarketController`, `MarketNewsController`) | Framework |
| 8 | `MapHub<MarketHub>("/hubs/market")` | Maps SignalR hub for live market price broadcasting | [MarketHub.cs](file:///c:/CryptoMarketProject/backend/Market.API/Hubs/MarketHub.cs) |
| 9 | `MapCustomHealthChecks()` | Exposes health check endpoints | Shared.Observability |

---

### 3. Notifications.API

Execution order in [Notifications.API/Program.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Program.cs):

| Order | Middleware | Purpose / Action | Source / Implementation |
|:---:|---|---|---|
| 1 | `UseCorrelationIdMiddleware()` | Attaches `X-Correlation-ID` | [CorrelationIdMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/CorrelationIdMiddleware.cs) |
| 2 | `UseLoggingMiddleware()` | Logs HTTP request timing and status | [LoggingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/LoggingMiddleware.cs) |
| 3 | `UseExceptionHandlingMiddleware()` | Global error formatting | [ExceptionHandlingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs) |
| 4 | `UseAuthentication()` | Validates JWT Bearer tokens (extracts from query string `access_token` for WebSocket) | Framework |
| 5 | `UseAuthorization()` | Enforces user authorization on notification and alert endpoints | Framework |
| 6 | `UseCors("SignalRPolicy")` | Permits origins `http://localhost:3000` and `http://localhost:5173` | Framework |
| 7 | `MapControllers()` | Dispatches to `NotificationController` and `PriceAlertController` | Framework |
| 8 | `MapHub<NotificationHub>("/hubs/notifications")` | Maps SignalR hub for user push notifications and alert triggers | [NotificationHub.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Hubs/NotificationHub.cs) |
| 9 | `MapCustomHealthChecks()` | Exposes health check endpoints | Shared.Observability |

---

### 4. Portfolio.API

Execution order in [Portfolio.API/Program.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Program.cs):

| Order | Middleware | Purpose / Action | Source / Implementation |
|:---:|---|---|---|
| 1 | `UseCors("AllowVueApp")` | Allows requests from `http://localhost:5173` | Framework |
| 2 | `UseCorrelationIdMiddleware()` | Propagates `X-Correlation-ID` header | [CorrelationIdMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/CorrelationIdMiddleware.cs) |
| 3 | `UseLoggingMiddleware()` | Records endpoint duration | [LoggingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/LoggingMiddleware.cs) |
| 4 | `UseExceptionHandlingMiddleware()` | Centralized try/catch error handling | [ExceptionHandlingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs) |
| 5 | `UseAuthentication()` | Validates JWT Bearer tokens | Framework |
| 6 | `UseAuthorization()` | Enforces authorization | Framework |
| 7 | `Inline Request Logger Delegate` | Reads request stream, computes SHA-256 hash of payload, logs `Idempotency-Key` and `X-Request-ID` | [Program.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Program.cs#L66-L84) |
| 8 | `UseIdempotencyMiddleware()` | Requires `Idempotency-Key` on POST/PUT/DELETE; verifies key in Redis (`Idemp_{key}`); rejects duplicates with 409 Conflict | [IdempotencyMiddleware.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Middlewares/IdempotencyMiddleware.cs) |
| 9 | `MapControllers()` | Dispatches to `WalletController` and `LimitOrderController` | Framework |
| 10 | `MapHub<PortfolioHub>("/hubs/portfolio")` | Maps SignalR hub for real-time portfolio balance push | [PortfolioHub.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Hubs/PortfolioHub.cs) |
| 11 | `MapCustomHealthChecks()` | Exposes health check endpoints | Shared.Observability |

---

### 5. API.Gateway

Execution order in [API.Gateway/Program.cs](file:///c:/CryptoMarketProject/backend/API.Gateway/Program.cs):

| Order | Middleware | Purpose / Action | Source / Implementation |
|:---:|---|---|---|
| 1 | `UseCors()` | Allows cross-origin requests | Framework |
| 2 | `MapReverseProxy()` | Executes YARP reverse proxy pipeline routing incoming paths to backend clusters | [appsettings.json](file:///c:/CryptoMarketProject/backend/API.Gateway/appsettings.json) |

---

## Sources

- [Identity.API/Program.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Program.cs)
- [Market.API/Program.cs](file:///c:/CryptoMarketProject/backend/Market.API/Program.cs)
- [Notifications.API/Program.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Program.cs)
- [Portfolio.API/Program.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Program.cs)
- [API.Gateway/Program.cs](file:///c:/CryptoMarketProject/backend/API.Gateway/Program.cs)
- [docs/decisions/0005-per-service-middleware.md](file:///c:/CryptoMarketProject/backend/docs/decisions/0005-per-service-middleware.md)
