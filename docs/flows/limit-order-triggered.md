# Limit Order Triggered Flow

> Cross-service flow initiated by simulated price ticks, triggering pending limit orders in Portfolio.API, executing trades, and dispatching real-time notifications via Notifications.API and SignalR.

---

## Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    %% Last verified against commit: b5229aa

    participant Sim as PriceSimulation (Market.API)
    participant MHub as MarketHub (SignalR)
    participant Bus as RabbitMQ (MassTransit)
    participant PConsumer as CoinPriceConsumer (Portfolio.API)
    participant PLimitService as LimitOrderService (Portfolio.API)
    participant Redis as Redis Cache (Portfolio.API)
    participant Repo as LimitOrderRepository (PortfolioDb)
    participant WalletSvc as WalletService (Portfolio.API)
    participant PHub as PortfolioHub (SignalR)
    participant NConsumer as LimitOrderConsumer (Notifications.API)
    participant NotifSvc as NotificationService (Notifications.API)
    participant NRepo as NotificationRepository (NotificationsDb)
    participant NHub as NotificationHub (SignalR)
    actor User as Client

    Note over Sim: BackgroundService periodic tick (every 2 seconds)
    Sim->>Sim: NextPrice calculation & MarketCap update

    par Broadcast live price over WebSocket
        Sim->>MHub: Clients.All.SendAsync("ReceivePriceUpdate", priceUpdateMessage)
    and Publish CoinPriceEvent to message broker
        Sim->>Bus: Publish(CoinPriceEvent { Price, Symbol })
    end

    Note over Bus,PConsumer: Queue: portfolio-coin-price-queue
    Bus->>PConsumer: Consume(CoinPriceEvent)
    PConsumer->>PLimitService: CheckLimitOrders(symbol, price)

    PLimitService->>Redis: GetOrdersAsync("{symbol}:Orders")
    alt Cache Miss
        Redis-->>PLimitService: null
        PLimitService->>Repo: FindAsync(x => x.Symbol == symbol)
        Repo-->>PLimitService: List<LimitOrder>
        PLimitService->>Redis: SetAsync("{symbol}:Orders", orders, 2m TTL)
    else Cache Hit
        Redis-->>PLimitService: Cached orders
    end

    PLimitService->>PLimitService: Filter Pending orders where IsTriggered(order, price)
    opt Triggered orders exist (Processed concurrently up to 4 parallel orders)
        PLimitService->>PLimitService: Mark matching orders as LimitOrderStatus.Processing
        PLimitService->>Redis: RefreshCacheAsync("{symbol}:Orders")

        PLimitService->>PLimitService: ApplyLimitOrder(dto, price)

        alt Buy Order with sufficient fiat balance
            PLimitService->>WalletSvc: BuyAsset(userId, symbol, price, amount, isLimitOrder: true)
            WalletSvc->>WalletSvc: LoadWalletWithAssetsAsync(userId)
            WalletSvc->>WalletSvc: Deduct FiatBalance, calculate Maker fee, add/update Asset
            WalletSvc->>PHub: Clients.User(userId).SendAsync("UpdatePortfolio", dashboard)
            WalletSvc->>Bus: Publish(FeeCollectionEvent)
            WalletSvc->>Bus: Publish(TradeExecuted)
            WalletSvc-->>PLimitService: Success
            PLimitService->>Bus: Publish(LimitOrderOccuredEvent { UserId, Symbol, Price, Amount, "Buy" })
            PLimitService->>Repo: UpdateAsync(order.Id, LimitOrderStatus.Filled)
        else Buy Order with insufficient fiat balance (FiatBalance < totalCost)
            PLimitService->>WalletSvc: BuyAsset(userId, symbol, price, amount, isLimitOrder: true)
            WalletSvc-->>PLimitService: throw ArgumentException("Error: Not Enough Money!")
            PLimitService->>PLimitService: Order execution aborted - order remains unfulfilled
        else Sell Order
            PLimitService->>WalletSvc: SellAsset(userId, symbol, price, amount, isLimitOrder: true)
            WalletSvc-->>PLimitService: Success
            PLimitService->>Bus: Publish(LimitOrderOccuredEvent { UserId, Symbol, Price, Amount, "Sell" })
            PLimitService->>Repo: UpdateAsync(order.Id, LimitOrderStatus.Filled)
        end

        PLimitService->>Redis: RefreshCacheAsync("{symbol}:Orders")
    end

    Note over Bus,NConsumer: Queue: notifications-limit-order-occurred-queue
    Bus->>NConsumer: Consume(LimitOrderOccuredEvent)
    NConsumer->>NotifSvc: CreateNotificationAsync(userId, "Your Limit Order occured", message, LimitOrderMatch)
    NotifSvc->>NRepo: AddAsync(Notification entity)
    NRepo-->>NotifSvc: Saved to NotificationsDb
    NotifSvc->>NHub: Clients.User(userId).SendAsync("ReceiveNotification", notification)
    NHub-->>User: Push real-time notification to client
```

---

## Process Description

1. The `PriceSimulation` background service in `Market.API` executes on a continuous loop (tick interval: 2 seconds), generating updated coin prices and market capitalization values based on trending models.
2. For each coin, `PriceSimulation` concurrently broadcasts a live price tick over WebSocket via `MarketHub.Clients.All.SendAsync("ReceivePriceUpdate", ...)` and publishes a `CoinPriceEvent` to RabbitMQ via MassTransit.
3. RabbitMQ delivers the message to the queue `portfolio-coin-price-queue`, where `CoinPriceConsumer` in `Portfolio.API` intercepts it and delegates to `LimitOrderService.CheckLimitOrders(symbol, price)`.
4. `LimitOrderService` checks Redis for cached active orders (`{symbol}:Orders`). On a cache miss, it retrieves orders from PostgreSQL `PortfolioDb` via `LimitOrderRepository.FindAsync` and writes the results to Redis with a 2-minute TTL.
5. The service inspects all pending orders matching the symbol, checking if current price crosses target limits (`Buy: currentPrice <= TargetPrice`, `Sell: currentPrice >= TargetPrice`).
6. If matching orders exist, their status is temporarily transitioned to `Processing` in cache to guard against duplicate pickup.
7. Orders are processed concurrently using `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 4`. For each order, `ApplyLimitOrder` is invoked.
8. For a Buy order, `WalletService.BuyAsset` verifies that `wallet.FiatBalance >= totalCost`. It applies maker fee calculations (`FeeSettings.MakerFeeRate`), debits fiat balance, credits asset quantity, logs a transaction record, dispatches a real-time portfolio update via `PortfolioHub`, and publishes `FeeCollectionEvent` and `TradeExecuted`.
9. Upon successful trade completion, `LimitOrderService` publishes `LimitOrderOccuredEvent` to RabbitMQ and marks the order as `Filled` in PostgreSQL `PortfolioDb`.
10. `Notifications.API` consumes `LimitOrderOccuredEvent` through `LimitOrderConsumer`. It records a notification in PostgreSQL `NotificationsDb` and immediately pushes the alert to the affected user via SignalR `NotificationHub`.

---

## Failure Paths

- **Insufficient Fiat Balance on Trigger:** If a user placed a Buy limit order but subsequently spent or withdrew their fiat balance before the order triggered, `WalletService.BuyAsset` throws `ArgumentException("Error: Not Enough Money!")`. The execution fails, and the order is not marked as `Filled`.
- **Insufficient Asset Balance for Sell Orders:** If a user placed a Sell limit order but transferred out the underlying crypto asset, `WalletService.SellAsset` throws `ArgumentException("Error: Not enough asset to sell or asset not found!")`.
- **Database or Redis Connectivity Fault:** If Redis is down during price checks, `GetOrdersAsync` falls back to querying `LimitOrderRepository` directly. If PostgreSQL is also unavailable, the consumer faults and MassTransit moves the event to `portfolio-coin-price-queue_error`.
- **SignalR Client Disconnection:** If the targeted user is disconnected or offline when notifications are generated, SignalR drops the transient push gracefully, but the notification remains safely stored in PostgreSQL `NotificationsDb` for retrieval upon next login via `GET /api/notifications`.

---

## Sources

- [PriceSimulation.cs](file:///c:/CryptoMarketProject/backend/Market.API/Infrastructure/BackgroundServices/PriceSimulation.cs#L22-L70) — Background service generating price updates and publishing `CoinPriceEvent`.
- [CoinPriceConsumer.cs (Portfolio.API)](file:///c:/CryptoMarketProject/backend/Portfolio.API/Consumers/CoinPriceConsumer.cs#L7-L14) — Consumer initiating limit order evaluation on price events.
- [LimitOrderService.cs (Portfolio.API)](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Services/LimitOrderService.cs#L19-L67) — Order matching logic and parallel trade execution.
- [WalletService.cs (Portfolio.API)](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Services/WalletService.cs#L109-L191) — Wallet balance verification, balance debit/credit, and trade publishing.
- [LimitOrderConsumer.cs (Notifications.API)](file:///c:/CryptoMarketProject/backend/Notifications.API/Consumers/LimitOrderConsumer.cs#L7-L19) — Consumer handling `LimitOrderOccuredEvent`.
- [NotificationService.cs (Notifications.API)](file:///c:/CryptoMarketProject/backend/Notifications.API/Application/Services/NotificationService.cs#L18-L27) — Notification persistence and SignalR delivery.
