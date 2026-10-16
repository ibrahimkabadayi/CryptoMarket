# API Endpoints Reference & OpenAPI Documentation

> Comprehensive catalog of HTTP endpoints, authentication requirements, request/response models, error contracts, and OpenAPI specifications across all CryptoMarket microservices.

---

## OpenAPI Specifications

Machine-readable OpenAPI 3.0 specifications for each microservice are exported in this directory:
- [openapi-identity.json](openapi-identity.json) — Identity & Authentication Service
- [openapi-market.json](openapi-market.json) — Market Data & AI News Service
- [openapi-notifications.json](openapi-notifications.json) — Push Notifications & Price Alerts Service
- [openapi-portfolio.json](openapi-portfolio.json) — Wallets, Asset Trading & Limit Orders Service

In local development, each microservice also serves its live OpenAPI document directly via ASP.NET Core OpenApi at `http://localhost:<port>/openapi/v1.json`.

---

## Error Handling & Response Contracts

The microservices return consistent JSON error structures based on standard HTTP status codes:

### 1. Global Exception Contract (Status 500 Internal Server Error)
Intercepted centrally by `Shared.Infrastructure.Middlewares.ExceptionHandlingMiddleware`:
```json
{
  "StatusCode": 500,
  "Message": "There was an error in the server, please try again.",
  "DetailedError": "Error description or stack message"
}
```

### 2. Missing Idempotency Key (Status 400 Bad Request)
Returned by `Portfolio.API/Middlewares/IdempotencyMiddleware` when `Idempotency-Key` header is omitted on mutating methods (`POST`, `PUT`, `DELETE`):
```json
{
  "Message": "For financial operations 'Idempotency-Key' header is mandotory."
}
```

### 3. Duplicate Idempotency Key (Status 409 Conflict)
Returned by `Portfolio.API/Middlewares/IdempotencyMiddleware` when the `Idempotency-Key` has already been processed within the last 24 hours:
```json
{
  "Message": "This transaction is either pending or already done."
}
```

### 4. Rate Limit Exceeded (Status 429 Too Many Requests)
Returned by `Identity.API/Middlewares/RateLimitingMiddleware` when an IP exceeds `RateLimitSettings:MaxRequests` (default 5 requests / 60 seconds):
```json
{
  "Message": "You have send too many request, please try again."
}
```

### 5. Authentication & Authorization Errors
- **401 Unauthorized:** Returned when an `[Authorize]` endpoint is invoked without a valid `Authorization: Bearer <jwt>` header, or when invalid credentials are provided to `/api/auth/login`.
- **404 Not Found:** Returned when the requested resource (coin ticker, notification, order, news item) does not exist.

---

## Master Endpoints Catalog

### 1. Identity.API (`:8001`)

Base route prefix: `/api/auth`

| Method | Route | Auth Required | Classification | Description | Request DTO / Params | Response DTO / Status | Source Controller |
|---|---|:---:|:---:|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Public | Registers a new user account and publishes `UserCreatedEvent` to RabbitMQ | `RegisterUserRequest` (`UserName`, `FirstName`, `LastName`, `Email`, `Password`) | `200 OK` (`{ Token, Message }`) or `400 BadRequest` | [AuthController.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Controllers/AuthController.cs#L16-L26) |
| `POST` | `/api/auth/login` | Anonymous | Public | Validates user credentials and issues signed JWT bearer token | `LoginRequest` (`Email`, `Password`) | `200 OK` (`{ Token }`) or `401 Unauthorized` | [AuthController.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Controllers/AuthController.cs#L29-L39) |
| `GET` | `/api/auth` | `[Authorize]` | JWT-Derived | Returns profile details of the authenticated caller extracted from JWT | None | `200 OK` (`UserDto` [Id, UserName, FirstName, LastName, Email]) or `401 Unauthorized`, `404 NotFound` | [AuthController.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Controllers/AuthController.cs#L42-L59) |

---

### 2. Market.API (`:8002`)

Base route prefixes: `/api/market`, `/api/market-news`

| Method | Route | Auth Required | Classification | Description | Request DTO / Params | Response DTO / Status | Source Controller |
|---|---|:---:|:---:|---|---|---|---|
| `GET` | `/api/market` | Anonymous | Public | Retrieves real-time prices, supply, and market cap for all supported coins (cached in Redis `market:coins`) | None | `200 OK` (`List<Coin>`) | [MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs#L47-L51) |
| `GET` | `/api/market/{symbol}` | Anonymous | Public | Retrieves real-time quote for a specific coin | Route: `symbol` (`string`) | `200 OK` (`Coin`) or `404 NotFound` | [MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs#L54-L63) |
| `GET` | `/api/market/{symbol}/history` | Anonymous | Public | Retrieves historical OHLCV candlestick bars for charting | Route: `symbol`<br>Query: `intervalMinutes` (default 5), `hoursBack` (default 24) | `200 OK` (`List<PriceHistoryDto>`) | [MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs#L66-L79) |
| `POST` | `/api/market/{symbol}` | `[Authorize]` | JWT-Derived | Initiates a market buy order; extracts `userId` from JWT and publishes `BuyCoinEvent` | Route: `symbol`<br>Body: `BuyCoinRequest` (`Amount`) | `200 OK` or `400 BadRequest` | [MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs#L24-L32) |
| `POST` | `/api/market/limit-order/{symbol}` | `[Authorize]` | JWT-Derived | Places a limit order from the market interface; extracts `userId` from JWT and publishes `LimitOrderPlacedEvent` | Route: `symbol`<br>Body: `SetLimitOrderRequest` (`TargetPrice`, `Amount`, `OrderType`) | `200 OK` or `400 BadRequest` | [MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs#L36-L44) |
| `GET` | `/api/market-news` | Anonymous | Public | Returns recent Gemini AI-generated market news articles | Query: `count` (default 10) | `200 OK` (`List<MarketNewsDto>`) | [MarketNewsController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketNewsController.cs#L17-L21) |
| `GET` | `/api/market-news/{id}` | Anonymous | Public | Returns a specific market news article by ID | Route: `id` (`Guid`) | `200 OK` (`MarketNewsDto`) or `404 NotFound` | [MarketNewsController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketNewsController.cs#L24-L33) |
| `GET` | `/api/market-news/coin/{symbol}` | Anonymous | Public | Returns recent market news relevant to a specific ticker | Route: `symbol`<br>Query: `count` (default 5) | `200 OK` (`List<MarketNewsDto>`) | [MarketNewsController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketNewsController.cs#L36-L40) |
| `DELETE` | `/api/market-news/{id}` | `[Authorize]` | Non-User | Deletes a market news article (administrative action) | Route: `id` (`Guid`) | `200 OK` (`{ message }`) or `404 NotFound` | [MarketNewsController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketNewsController.cs#L43-L52) |

---

### 3. Notifications.API (`:6000`)

Base route prefixes: `/api/notifications`, `/api/price-alerts`

| Method | Route | Auth Required | Classification | Description | Request DTO / Params | Response DTO / Status | Source Controller |
|---|---|:---:|:---:|---|---|---|---|
| `GET` | `/api/notifications` | `[Authorize]` | JWT-Derived | Returns in-app notifications for authenticated user | None | `200 OK` (`List<NotificationDto>`) | [NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs#L18-L23) |
| `GET` | `/api/notifications/unread-count` | `[Authorize]` | JWT-Derived | Returns count of unread notifications | None | `200 OK` (`int`) | [NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs#L26-L31) |
| `PUT` | `/api/notifications/{notificationId}/read` | `[Authorize]` | JWT-Derived | Marks a specific notification as read | Route: `notificationId` (`Guid`) | `204 NoContent` | [NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs#L34-L40) |
| `PUT` | `/api/notifications/mark-all-read` | `[Authorize]` | JWT-Derived | Marks all notifications for user as read | None | `204 NoContent` | [NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs#L43-L48) |
| `POST` | `/api/notifications` | `[Authorize]` | JWT-Derived | Creates an internal user notification (service or admin trigger) | Body: `CreateNotificationRequest` (`Title`, `Message`, `Type`, `RelatedEntityId`) | `200 OK` (`{ Message }`) | [NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs#L51-L77) |
| `POST` | `/api/price-alerts` | `[Authorize]` | JWT-Derived | Registers a target price alert on a coin; extracts `userId` from JWT | Body: `CreatePriceAlertRequest` (`Symbol`, `TargetPrice`, `IsAbove`) | `200 OK` (`{ Message }`) | [PriceAlertController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/PriceAlertController.cs#L18-L38) |
| `GET` | `/api/price-alerts/active` | `[Authorize]` | JWT-Derived | Returns caller's active price alerts | None | `200 OK` (`List<PriceAlertDto>`) | [PriceAlertController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/PriceAlertController.cs#L41-L46) |
| `GET` | `/api/price-alerts/all` | `[Authorize]` | JWT-Derived | Returns all caller's price alerts (active and triggered) | None | `200 OK` (`List<PriceAlertDto>`) | [PriceAlertController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/PriceAlertController.cs#L49-L54) |
| `PUT` | `/api/price-alerts/{priceAlertId}/deactivate` | `[Authorize]` | JWT-Derived | Deactivates an existing price alert | Route: `priceAlertId` (`Guid`) | `204 NoContent` | [PriceAlertController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/PriceAlertController.cs#L57-L69) |

---

### 4. Portfolio.API (`:7000`)

Base route prefixes: `/api/wallet`, `/api/limit-orders`

| Method | Route | Auth Required | Classification | Description | Request DTO / Headers | Response DTO / Status | Source Controller |
|---|---|:---:|:---:|---|---|---|---|
| `GET` | `/api/wallet` | `[Authorize]` | JWT-Derived | Returns user portfolio overview, fiat balance, coin holdings, and recent 10 transactions | None | `200 OK` (`{ result: PortfolioDashboardDto }`) | [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L23-L37) |
| `POST` | `/api/wallet/deposits` | `[Authorize]` | JWT-Derived | Credits fiat currency balance to user wallet | Header: `Idempotency-Key`<br>Body: `DepositMoneyRequest` (`Amount`) | `200 OK` (`{ Message }`) | [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L40-L59) |
| `POST` | `/api/wallet/withdrawals` | `[Authorize]` | JWT-Derived | Debits fiat balance from user wallet (validates sufficient balance) | Header: `Idempotency-Key`<br>Body: `WithdrawMoneyRequest` (`Amount`) | `200 OK` | [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L62-L80) |
| `POST` | `/api/wallet/assets/{symbol}/purchases` | `[Authorize]` | JWT-Derived | Executes immediate market asset purchase, checks fiat balance, deducts taker fee, records transaction | Route: `symbol`<br>Header: `Idempotency-Key`<br>Body: `BuyAssetRequest` (`Amount`, `Price`) | `200 OK` (`string`) | [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L83-L99) |
| `POST` | `/api/wallet/assets/{symbol}/sales` | `[Authorize]` | JWT-Derived | Sells asset quantity, credits fiat balance minus maker/taker fee, records transaction | Route: `symbol`<br>Header: `Idempotency-Key`<br>Body: `SellAssetRequest` (`Amount`, `Price`) | `200 OK` | [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L102-L117) |
| `POST` | `/api/wallet/assets/{symbol}/transfers` | `[Authorize]` | JWT-Derived | Transfers crypto asset quantity to another wallet address; publishes `AssetTransferEvent` | Route: `symbol`<br>Header: `Idempotency-Key`<br>Body: `TransferAssetRequest` (`ReceiverWalletAddress`, `Amount`) | `200 OK` (`{ Message }`) | [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L120-L141) |
| `POST` | `/api/limit-orders` | `[Authorize]` | JWT-Derived | Creates a new limit buy/sell order; extracts `userId` from JWT and guards with Redis CorrelationId | Header: `Idempotency-Key`<br>Body: `CreateLimitOrderRequest` (`WalletId`, `Symbol`, `TargetPrice`, `Amount`, `OrderType`) | `200 OK` | [LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs#L17-L43) |
| `GET` | `/api/limit-orders/{id}` | `[Authorize]` | Non-User / Entity | Returns single limit order by order ID | Route: `id` (`Guid`) | `200 OK` (`LimitOrderDto`) | [LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs#L46-L50) |
| `GET` | `/api/limit-orders` | `[Authorize]` | Non-User / Entity | Returns all limit orders across system | None | `200 OK` (`List<LimitOrderDto>`) | [LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs#L53-L57) |
| `PATCH` | `/api/limit-orders/{id}` | `[Authorize]` | Non-User / Entity | Updates target price or amount of an existing pending limit order | Route: `id` (`Guid`)<br>Header: `Idempotency-Key`<br>Body: `UpdateLimitOrderRequest` (`TargetPrice`, `Amount`) | `200 OK` (`string`) | [LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs#L60-L64) |
| `DELETE` | `/api/limit-orders/{id}` | `[Authorize]` | Non-User / Entity | Cancels (hard-deletes) a limit order from `PortfolioDb` | Route: `id` (`Guid`)<br>Header: `Idempotency-Key` | `200 OK` | [LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs#L67-L72) |

---

## SignalR Real-Time WebSocket Hubs

| Hub Class | Service | Route / URL | Authentication | Events Dispatched |
|---|---|---|---|---|
| `MarketHub` | `Market.API` | `/hubs/market` | Anonymous | `ReceivePriceUpdate` (`Coin` object broadcast every second from `PriceSimulation`) |
| `NotificationHub` | `Notifications.API` | `/hubs/notifications?access_token=<jwt>` | `[Authorize]` via Query String | `ReceiveNotification` (`NotificationDto`), `ReceivePriceAlert` (`PriceAlertDto`) |
| `PortfolioHub` | `Portfolio.API` | `/hubs/portfolio?access_token=<jwt>` | `[Authorize]` via Query String | `ReceivePortfolioUpdate` (`PortfolioDashboardDto`) |

---

## Development & Diagnostic Endpoints

All services register health checks and OpenAPI reflection endpoints in Development:
- **Health Checks:**
  - `GET /health/live` — Liveness probe (returns `Healthy` / `200 OK` if container is alive).
  - `GET /health/ready` — Readiness probe (validates database, RabbitMQ, and Redis connectivity).
- **OpenAPI Schema:**
  - `GET /openapi/v1.json` — Returns live JSON schema generated by Microsoft.AspNetCore.OpenApi.
- **Sample Request Files:**
  - Pre-built `.http` request scripts for manual testing with VS Code or Rider are located in [API.Gateway/IdentityRequests/](file:///c:/CryptoMarketProject/backend/API.Gateway/IdentityRequests/).

---

## Sources

- [Identity.API/Controllers/AuthController.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Controllers/AuthController.cs)
- [Market.API/Controllers/MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs)
- [Market.API/Controllers/MarketNewsController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketNewsController.cs)
- [Notifications.API/Controllers/NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs)
- [Notifications.API/Controllers/PriceAlertController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/PriceAlertController.cs)
- [Portfolio.API/Controllers/WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs)
- [Portfolio.API/Controllers/LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs)
- [Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/ExceptionHandlingMiddleware.cs)
- [Portfolio.API/Middlewares/IdempotencyMiddleware.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Middlewares/IdempotencyMiddleware.cs)
- [Identity.API/Middlewares/RateLimitingMiddleware.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Middlewares/RateLimitingMiddleware.cs)
