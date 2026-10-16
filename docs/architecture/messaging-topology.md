# Messaging Topology

> Message broker topology for MassTransit over RabbitMQ: producers, exchanges, queues, consumer subscriptions, and fault queues.

---

## Topology Diagram

The platform utilizes MassTransit publish-subscribe messaging over RabbitMQ. By convention, MassTransit generates fanout topic exchanges named after the fully qualified .NET message contract type, which are then bound to named consumer queues.

```mermaid
flowchart LR
    %% Last verified against commit: b5229aa

    subgraph Producers ["Event Producers"]
        P_Identity["Identity.API"]
        P_Market["Market.API"]
        P_Portfolio["Portfolio.API"]
    end

    subgraph Exchanges ["MassTransit RabbitMQ Exchanges"]
        Ex_UserCreated["Shared.Messages:UserCreatedEvent"]
        Ex_CoinPrice["Shared.Messages:CoinPriceEvent"]
        Ex_LimitOrderPlaced["Shared.Messages:LimitOrderPlacedEvent"]
        Ex_LimitOrderOccured["Shared.Messages:LimitOrderOccuredEvent"]
        Ex_AssetTransfer["Shared.Messages:AssetTransferEvent"]
        Ex_BuyCoin["Shared.Messages:BuyCoinEvent"]
        Ex_FeeCollection["Shared.Messages:FeeCollectionEvent"]
        Ex_AssetDeposited["Shared.Messages:AssetDeposited"]
        Ex_AssetWithdrawn["Shared.Messages:AssetWithdrawn"]
        Ex_TradeExecuted["Shared.Messages:TradeExecuted"]
    end

    subgraph Queues ["RabbitMQ Receive Endpoints"]
        Q_Pf_User["portfolio-user-created-queue"]
        Q_No_User["notification-user-created-queue"]
        Q_Pf_Price["portfolio-coin-price-queue"]
        Q_No_Price["notification-coin-price-queue"]
        Q_Pf_LimitPlaced["portfolio-set-limit-order-queue"]
        Q_No_LimitOccured["notification-limit-order-queue"]
        Q_No_Transfer["notification-asset-transferred-queue"]
        Q_Pf_BuyCoin["portfolio-buy-coin-order-queue"]
        Q_Pf_Fee["portfolio-fee-collection-queue"]
        Q_External["External Subscriber Queues"]
    end

    subgraph ErrorQueues ["Fault / Dead-Letter Queues"]
        Q_Faults["[queue-name]_error<br/>(MassTransit fault queues)"]
    end

    subgraph Consumers ["Consumers"]
        C_Pf_User["Portfolio.API:<br/>UserCreatedConsumer"]
        C_No_User["Notifications.API:<br/>UserCreatedConsumer"]
        C_Pf_Price["Portfolio.API:<br/>CoinPriceConsumer"]
        C_No_Price["Notifications.API:<br/>CoinPriceConsumer"]
        C_Pf_LimitPlaced["Portfolio.API:<br/>SetLimitOrderEvent"]
        C_No_LimitOccured["Notifications.API:<br/>LimitOrderConsumer"]
        C_No_Transfer["Notifications.API:<br/>AssetTransferConsumer"]
        C_Pf_BuyCoin["Portfolio.API:<br/>BuyCoinConsumer"]
        C_Pf_Fee["Portfolio.API:<br/>FeeCollectionConsumer"]
        C_External["External Project Consumers"]
    end

    %% Producer to Exchange Links
    P_Identity -->|Publish| Ex_UserCreated
    P_Market -->|Publish| Ex_CoinPrice
    P_Market -->|Publish| Ex_LimitOrderPlaced
    P_Market -->|Publish| Ex_BuyCoin
    P_Portfolio -->|Publish| Ex_LimitOrderOccured
    P_Portfolio -->|Publish| Ex_AssetTransfer
    P_Portfolio -->|Publish| Ex_FeeCollection
    P_Portfolio -->|Publish| Ex_AssetDeposited
    P_Portfolio -->|Publish| Ex_AssetWithdrawn
    P_Portfolio -->|Publish| Ex_TradeExecuted

    %% Exchange to Queue Bindings
    Ex_UserCreated --> Q_Pf_User
    Ex_UserCreated --> Q_No_User
    Ex_CoinPrice --> Q_Pf_Price
    Ex_CoinPrice --> Q_No_Price
    Ex_LimitOrderPlaced --> Q_Pf_LimitPlaced
    Ex_LimitOrderOccured --> Q_No_LimitOccured
    Ex_AssetTransfer --> Q_No_Transfer
    Ex_BuyCoin --> Q_Pf_BuyCoin
    Ex_FeeCollection --> Q_Pf_Fee
    Ex_AssetDeposited & Ex_AssetWithdrawn & Ex_TradeExecuted --> Q_External

    %% Queue to Consumer Dispatch
    Q_Pf_User --> C_Pf_User
    Q_No_User --> C_No_User
    Q_Pf_Price --> C_Pf_Price
    Q_No_Price --> C_No_Price
    Q_Pf_LimitPlaced --> C_Pf_LimitPlaced
    Q_No_LimitOccured --> C_No_LimitOccured
    Q_No_Transfer --> C_No_Transfer
    Q_Pf_BuyCoin --> C_Pf_BuyCoin
    Q_Pf_Fee --> C_Pf_Fee
    Q_External --> C_External

    %% Fault routing
    Q_Pf_User & Q_No_User & Q_Pf_Price & Q_No_Price & Q_Pf_LimitPlaced & Q_No_LimitOccured & Q_No_Transfer & Q_Pf_BuyCoin & Q_Pf_Fee -.->|On unhandled exception| Q_Faults

    classDef prod fill:#dbeafe,stroke:#1d4ed8,color:#1e3a8a,rx:4
    classDef exch fill:#fef3c7,stroke:#d97706,color:#78350f,rx:4
    classDef queue fill:#dcfce7,stroke:#16a34a,color:#14532d,rx:4
    classDef consumer fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,rx:4
    classDef fault fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,rx:4

    class P_Identity,P_Market,P_Portfolio prod
    class Ex_UserCreated,Ex_CoinPrice,Ex_LimitOrderPlaced,Ex_LimitOrderOccured,Ex_AssetTransfer,Ex_BuyCoin,Ex_FeeCollection,Ex_AssetDeposited,Ex_AssetWithdrawn,Ex_TradeExecuted exch
    class Q_Pf_User,Q_No_User,Q_Pf_Price,Q_No_Price,Q_Pf_LimitPlaced,Q_No_LimitOccured,Q_No_Transfer,Q_Pf_BuyCoin,Q_Pf_Fee,Q_External queue
    class C_Pf_User,C_No_User,C_Pf_Price,C_No_Price,C_Pf_LimitPlaced,C_No_LimitOccured,C_No_Transfer,C_Pf_BuyCoin,C_Pf_Fee,C_External consumer
    class Q_Faults fault
```

*Solid lines represent happy-path message routing; dashed lines represent fault routing to MassTransit error queues upon unhandled exceptions.*

---

## Topology Rules & Conventions

1. **Exchange Naming:** MassTransit automatically names RabbitMQ exchanges after the fully qualified CLR type of the message contract:
   - Pattern: `<Namespace>:<TypeName>` (e.g. `Shared.Messages:CoinPriceEvent`).
2. **Exchange Type:** All published message contracts are declared as `fanout` topic exchanges in RabbitMQ. If multiple queues bind to the exchange (e.g., both Portfolio and Notifications bind to `UserCreatedEvent` and `CoinPriceEvent`), each queue receives a dedicated copy of the message.
3. **Queue Naming:** Endpoints are configured explicitly in `Program.cs` via `cfg.ReceiveEndpoint("kebab-case-queue-name", ...)` to ensure predictable queue names across environments.
4. **Fault & Error Queues:**
   - When a consumer throws an unhandled exception, MassTransit catches the error and moves the original message along with exception details to a dead-letter queue named `[queue-name]_error`.
   - Examples: `portfolio-set-limit-order-queue_error`, `notification-limit-order-queue_error`.
5. **External Subscriptions:** `AssetDeposited`, `AssetWithdrawn`, and `TradeExecuted` are published to the broker for consumption by downstream services external to this repository.

---

## Sources

- `Shared.Messages/*.cs`
- `Identity.API/Program.cs`
- `Market.API/Program.cs`
- `Portfolio.API/Program.cs`
- `Notifications.API/Program.cs`
- `docs/events.md`
- `docs/_inventory.md` — Section 4 (RabbitMQ Messaging)
