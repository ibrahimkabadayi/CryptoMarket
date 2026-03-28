# Crypto Market Microservices

A robust real-time cryptocurrency market application built with a modern microservices architecture, featuring secure authentication, market data management, portfolio tracking, and real-time notifications.

## Architecture Overview

The project is designed using **Microservices Architecture** with a focus on scalability, decoupling, and high availability. It utilizes an **API Gateway** as the single entry point and asynchronous messaging for inter-service communication.

```mermaid
  graph TB
    subgraph clients["Client Layer"]
        W["Web UI"]
        M["Mobile UI"]
    end

    W & M --> GW["API Gateway"]

    GW --> RMQ

    subgraph backend["Microservices Layer"]
        RMQ[["RabbitMQ"]]

        RMQ <--> ID["Identity Service"]
        RMQ <--> NO["Notification Service"]
        RMQ <--> MK["Market Service"]
        RMQ <--> PF["Portfolio Service"]
    end

    ID  --> PG1[("PostgreSQL")]
    NO  --> PG2[("PostgreSQL")]
    MK  --> MG[("MongoDB")]
    MK  --> RD1[("Redis")]
    PF  --> PG3[("PostgreSQL")]
    PF  --> RD2[("Redis")]

    classDef ui       fill:#dbeafe,stroke:#3b82f6,color:#1e3a8a,rx:8
    classDef gw       fill:#fef9c3,stroke:#eab308,color:#713f12
    classDef bus      fill:#ede9fe,stroke:#7c3aed,color:#3b0764
    classDef svc      fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef postgres fill:#fff7ed,stroke:#ea580c,color:#7c2d12
    classDef mongo    fill:#fef2f2,stroke:#dc2626,color:#7f1d1d
    classDef redis    fill:#fdf4ff,stroke:#a21caf,color:#581c87

    class W,M ui
    class GW gw
    class RMQ bus
    class ID,NO,MK,PF svc
    class PG1,PG2,PG3 postgres
    class MG mongo
    class RD1,RD2 redis
```

###  Services Breakdown

-   **API Gateway**: Built with **YARP (Yet Another Reverse Proxy)**. It routes incoming requests to the appropriate microservices, providing a unified entry point.
-   **Identity.API**: Handles user authentication, registration, and authorization using **ASP.NET Core Identity** and **PostgreSQL**. It publishes events when new users are created.
-   **Market.API**: Manages cryptocurrency market data (coins, prices). It uses **MongoDB** for high-performance document storage.
-   **Portfolio.API**: Tracks user wallets and asset balances. It consumes events from `Identity.API` to initialize wallets and manages asset transfers via **PostgreSQL**.
-   **Notifications.API**: Processes and sends notifications (e.g., asset transfer confirmations). It consumes events from the message bus.
-   **Shared.Messages**: A common library containing shared event records used for **MassTransit** messaging.

###  Inter-service Communication

Services communicate asynchronously using **MassTransit** over **RabbitMQ**:
-   `UserCreatedEvent`: Published by `Identity.API` when a user registers; consumed by `Portfolio.API` to create an initial wallet.
-   `AssetTransferEvent`: Consumed by `Notifications.API` to trigger user notifications.

#### Real-time Trading Engine & Limit Order Flow

To prevent database bottlenecks during high-frequency price updates, the system utilizes a hybrid approach combining RabbitMQ streams and Redis in-memory caching.

```mermaid
graph LR
    subgraph Market API [Market.API Bounded Context]
        direction TB
        MarketMongo[(MongoDB<br/>MarketDb)]
        MarketRedis[(Redis<br/>Coin Cache)]
        PriceSim[PriceSimulation<br/>BackgroundService]

        MarketMongo -->|1. Reads Coins| PriceSim
        PriceSim -->|2. Updates Price| MarketRedis
    end

    subgraph Message Broker
        RMQ>RabbitMQ<br/>Event Bus]
    end

    PriceSim -->|3. Publish: CoinPriceEvent| RMQ

    subgraph Portfolio API
        direction TB
        PortPgSQL[(PostgreSQL<br/>PortfolioDb)]
        PortRedis[(Redis<br/>Limit Order Cache)]
        OrderSvc[LimitOrderController<br/>API Service]
        Consumer[PriceEventConsumer<br/>BackgroundService]

        OrderSvc -->|A. Save Order & Lock Funds| PortPgSQL
        OrderSvc -->|B. Cache Target Price| PortRedis

        Consumer -->|C. Check Target Price| PortRedis
        Consumer -->|D. Execute Trade & Update Balance| PortPgSQL
    end

    RMQ -->|4. Consume: CoinPriceEvent| Consumer
    
    classDef database fill:#f9f9f9,stroke:#333,stroke-width:2px;
    classDef service fill:#e1f5fe,stroke:#0288d1,stroke-width:2px;
    classDef broker fill:#fff3e0,stroke:#f57c00,stroke-width:2px;
    
    class MarketMongo,MarketRedis,PortPgSQL,PortRedis database;
    class PriceSim,OrderSvc,Consumer service;
    class RMQ broker;
```

As illustrated above, `Market.API` continuously streams price events via RabbitMQ. `Portfolio.API` consumes these events and evaluates them against active limit orders stored in a local Redis cache, ensuring PostgreSQL is only queried when a trade execution is strictly required.

##  Technology Stack

-   **Runtime**: .NET 9.0
-   **API Gateway**: YARP
-   **Databases**: 
    -   **PostgreSQL** (Identity, Portfolio)
    -   **MongoDB** (Market)
    -   **Redis** (Distributed Caching)
-   **Messaging**: MassTransit with RabbitMQ
-   **Containerization**: Docker & Docker Compose
-   **CI/CD**: GitHub Actions

##  Getting Started

### Prerequisites

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Docker Desktop](https://www.docker.com/products/docker-desktop)
-   An IDE (Rider, Visual Studio, or VS Code)

### Installation & Running

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/Identity.API.git
    cd Identity.API
    ```

2.  **Run with Docker Compose:**
    The easiest way to start the entire infrastructure is using Docker Compose:
    ```bash
    docker-compose up -d
    ```
    This will spin up all microservices along with PostgreSQL, MongoDB, Redis, and RabbitMQ.

3.  **Accessing the services:**
    -   **API Gateway**: `http://localhost:5000`
    -   **RabbitMQ Management**: `http://localhost:15672` (Guest/Guest)
    -   **PostgreSQL**: `localhost:5432`
    -   **MongoDB**: `localhost:27107`

##  API Routes (via Gateway)

| Service | Path Prefix | Description |
| :--- | :--- | :--- |
| **Identity** | `/api/Auth/` | Registration, Login, User Management |
| **Market** | `/api/market/` | Coin listings and market data |
| **Portfolio** | `/api/Wallet/` | User balances and transfers |
| **Notifications** | `/api/notification/` | User notification history |

##  Development

### Running Migrations
For services using PostgreSQL (Identity and Portfolio), ensure migrations are applied:
```bash
dotnet ef database update --project Identity.API
dotnet ef database update --project Portfolio.API
```
