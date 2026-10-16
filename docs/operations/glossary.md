# Glossary

> Definitions of domain concepts, technical patterns, and project-specific terminology used across CryptoMarket.

---

## Domain & Financial Terms

| Term | Definition |
|---|---|
| **Aggregate Root** | A domain-driven design pattern where a single entity (e.g., `Wallet` or `Coin`) encapsulates and controls mutations to internal child entities (`Asset`, `PriceHistory`) to enforce consistency invariants. |
| **Asset** | A specific cryptocurrency holding owned by a `Wallet` in `PortfolioDb`, defined by ticker `Symbol`, held `Quantity`, and cumulative `CostBasis`. |
| **Candlestick (OHLCV)** | A financial time-series representation tracking Open, High, Low, Close prices, and total trading Volume over a discrete time window (e.g., 5-minute intervals). |
| **Coin** | The core market reference entity in `MarketDb`, representing a tradable cryptocurrency with name, ticker symbol, current market price, and circulating supply. |
| **Cost Basis** | The cumulative weighted average purchase cost of an asset holding, used to compute unrealized profit/loss upon sale. |
| **Fiat Balance** | The standard sovereign currency balance (USD equivalent) stored in a user's `Wallet` for executing market buys or receiving withdrawal payouts. |
| **Limit Order** | An order placed by a user to buy or sell a specified quantity of a coin once market price reaches or breaches a designated target price (`TargetPrice`). |
| **Maker Fee** | The platform trading fee rate applied to passive liquidity orders (e.g., limit orders filled against the order book), configured as 0.1% (`MakerFeeRate = 0.001`). |
| **Market Cap** | The aggregate market valuation of a cryptocurrency, calculated continuously as `CurrentPrice * Supply`. |
| **Taker Fee** | The platform trading fee rate applied to immediate market orders that execute instantly against existing liquidity, configured as 0.2% (`TakerFeeRate = 0.002`). |
| **Treasury Balance** | Platform fee earnings collected in coin asset units or fiat from trade fee deductions, stored in `PortfolioDb.TreasuryBalances`. |
| **Wallet** | The primary user account aggregate root in `Portfolio.API`, managing fiat balance, unique address, cryptographic optimistic lock tokens, and crypto asset positions. |

---

## Technical & Architectural Terms

| Term | Definition |
|---|---|
| **CorrelationId** | A unique GUID attached to an asynchronous message contract or HTTP request header (`X-Correlation-ID`) used to trace execution flows across microservices and prevent duplicate message processing. |
| **Idempotency Key** | A unique token supplied via `Idempotency-Key` HTTP header on mutating financial operations (`POST`, `PUT`, `DELETE`). Cached in Redis (`Idemp_{key}`) with a 24-hour TTL to prevent double-spending or duplicate order placements upon network retry. |
| **MassTransit** | An open-source message bus framework for .NET providing high-level abstraction over RabbitMQ exchanges, message serializers, retry policies, and consumer endpoints. |
| **Optimistic Concurrency** | A concurrency control mechanism using the `RowVersion` uint column on `Wallet` entities to prevent race conditions without holding persistent database table locks. |
| **Reverse Proxy (YARP)** | Yet Another Reverse Proxy, used in `API.Gateway` to route incoming public HTTP and SignalR WebSocket requests to internal backend service containers. |
| **SignalR Hub** | A real-time bidirectional communication abstraction in ASP.NET Core allowing servers to push price updates, portfolio changes, and notifications to connected browser clients over WebSockets. |
| **Stub Entity** | An entity depicted in an ER diagram with only primary key `Id` and an ownership label, representing data owned by another microservice across database boundaries. |

---

## Sources

- [Portfolio.API/Domain/Entities/Wallet.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Domain/Entities/Wallet.cs)
- [Market.API/Domain/Entities/Coin.cs](file:///c:/CryptoMarketProject/backend/Market.API/Domain/Entities/Coin.cs)
- [Portfolio.API/Middlewares/IdempotencyMiddleware.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Middlewares/IdempotencyMiddleware.cs)
- [Shared.Messages](file:///c:/CryptoMarketProject/backend/Shared.Messages/)
- [Shared.Observability/Middlewares/CorrelationIdMiddleware.cs](file:///c:/CryptoMarketProject/backend/Shared.Observability/Middlewares/CorrelationIdMiddleware.cs)
