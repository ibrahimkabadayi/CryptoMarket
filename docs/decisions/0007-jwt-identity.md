# 0007 - User identity is always derived from the JWT, never from the client

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

In earlier iterations of the codebase, some HTTP routes and message contracts accepted `userId` directly from route parameters or request payload bodies (e.g., `/api/auth/{userId}`, `/api/wallet/assets/{symbol}/purchases`, or `request.UserId`). This introduced severe security and authorization vulnerabilities: an authenticated caller could tamper with the request to impersonate or manipulate another user's wallet balances, limit orders, or notification settings simply by passing that user's `Guid`.

## Decision

User identity is **always** extracted directly from validated JWT claims (`User.FindFirstValue(ClaimTypes.NameIdentifier)`) on every user-scoped endpoint. Clients are forbidden from supplying `userId` in routes, query parameters, or request payloads. Endpoints that do not derive identity from JWTs are strictly anonymous public endpoints (e.g., login, registration, public market tickers, and candlestick data).

## Alternatives considered

- **Accepting `userId` with Route/Claim Verification:** Allowing `userId` in the route or body, but verifying that it matches the JWT claim. Rejected as redundant and error-prone, as it creates unnecessary failure modes and leaves open risks if verification is omitted in new endpoints.

## Consequences

- (+) **Elimination of IDOR (Insecure Direct Object Reference) Vulnerabilities:** An authenticated user can only operate against their own wallet, assets, limit orders, and alerts. Impersonation via parameter tampering is structurally impossible.
- (+) **Cleaner API Contracts:** Request DTOs and routes no longer require redundant `userId` properties.
- (+) **Consistent Authorization Pattern:** Standardized usage of `User.FindFirstValue(ClaimTypes.NameIdentifier)` across all microservices (`Identity.API`, `Market.API`, `Portfolio.API`, `Notifications.API`).
- (-) **Token Requirement for Testing:** Direct manual API calls against user-scoped endpoints in development require a valid Bearer token obtained via `/api/auth/login`.

## Evidence

Verified against commit `be4cbee`:
1. **Identity.API:**
   - `AuthController.GetUser` (`GET /api/auth`): Reads `User.FindFirstValue(ClaimTypes.NameIdentifier)`. (Previous route `/api/auth/{userId}` was replaced).
2. **Market.API:**
   - `MarketController.BuyCoin` (`POST /api/market/{symbol}`): Extracts `userId` from JWT claim before dispatching `BuyCoinEvent`.
   - `MarketController.SetLimitOrder` (`POST /api/market/limit-order/{symbol}`): Extracts `userId` from JWT claim before dispatching `LimitOrderPlacedEvent`.
3. **Portfolio.API:**
   - `LimitOrderController.CreateLimitOrder` (`POST /api/limit-orders`): Extracts `userId` from JWT claim (`ClaimTypes.NameIdentifier`).
   - `WalletController`: All actions (`GET /api/wallet`, `POST /api/wallet/withdrawals`, `POST /api/wallet/deposits`, `POST /api/wallet/assets/{symbol}/purchases`, `POST /api/wallet/assets/{symbol}/transfers`, `POST /api/wallet/assets/{symbol}/sales`) extract `userId` from JWT claims.
4. **Notifications.API:**
   - `NotificationController`: All actions (`GET /api/notifications`, `GET /api/notifications/unread-count`, `PUT /api/notifications/{id}/read`, `PUT /api/notifications/mark-all-read`, `POST /api/notifications`) extract `userId` from JWT claims.
   - `PriceAlertController`: All actions (`POST /api/price-alerts`, `GET /api/price-alerts/active`, `GET /api/price-alerts/all`, `PUT /api/price-alerts/{id}/deactivate`) extract `userId` from JWT claims.
5. **Audit Result:** Exactly 0 endpoints fall into category (c) ("user-scoped but identity comes from somewhere other than JWT"). Every user-scoped endpoint in the codebase derives identity from JWT.

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L77-L87) — Section 0007.

---

## Sources

- [Identity.API/Controllers/AuthController.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Controllers/AuthController.cs#L41-L59)
- [Market.API/Controllers/MarketController.cs](file:///c:/CryptoMarketProject/backend/Market.API/Controllers/MarketController.cs#L23-L44)
- [Portfolio.API/Controllers/LimitOrderController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/LimitOrderController.cs#L17-L43)
- [Portfolio.API/Controllers/WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L22-L135)
- [Notifications.API/Controllers/NotificationController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/NotificationController.cs#L17-L77)
- [Notifications.API/Controllers/PriceAlertController.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Controllers/PriceAlertController.cs#L17-L69)
