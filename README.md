<div align="center">

# CryptoMarket

A modern, real-time cryptocurrency market platform built with .NET 9 microservices, asynchronous event-driven messaging, and high-frequency trading simulation.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-MassTransit-FF6600?style=flat-square&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Redis](https://img.shields.io/badge/Redis-6.2-DC382D?style=flat-square&logo=redis&logoColor=white)](https://redis.io/)
[![YARP](https://img.shields.io/badge/Gateway-YARP-0078D4?style=flat-square&logo=microsoft&logoColor=white)](https://microsoft.github.io/reverse-proxy/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

</div>

---

## Key Features

- **Microservices Architecture:** 4 domain services (Identity, Market, Portfolio, Notifications) isolated behind a single YARP API Gateway.
- **Real-Time Market Simulation:** Live price and supply simulation background services streaming tick data via SignalR WebSockets and RabbitMQ.
- **Automated Limit Order Engine:** Fast limit order price matching against real-time streams utilizing Redis for fast evaluation.
- **Asynchronous Event-Driven Bus:** Reliable inter-service workflows orchestrated via MassTransit over RabbitMQ.
- **Database-per-Service:** Complete physical data isolation with independent PostgreSQL databases per microservice.
- **AI-Powered Market News:** Automated synthetic market news generation integrated with Google Gemini AI.
- **End-to-End Observability:** Distributed tracing across all HTTP endpoints and RabbitMQ message hops via OpenTelemetry and Jaeger.

---

## Architecture

All client requests enter through the API Gateway, while microservices communicate asynchronously over the event bus:

```mermaid
flowchart TD
    %% Last verified against commit: be4cbee

    subgraph Clients ["Clients"]
        Web["Web UI (Vue 3)"]
        Mobile["Mobile App"]
    end

    Gateway["API Gateway (YARP :5000)"]

    subgraph Services ["Microservices (.NET 9)"]
        direction LR
        Identity["Identity.API<br/>:8001"]
        Market["Market.API<br/>:8002"]
        Portfolio["Portfolio.API<br/>:7000"]
        Notifications["Notifications.API<br/>:6000"]
    end

    subgraph Infrastructure ["Backing Infrastructure"]
        direction LR
        Bus[["RabbitMQ Event Bus"]]
        Cache[("Redis Cache")]
        DBs[("PostgreSQL (4 DBs)")]
    end

    Web & Mobile --> Gateway
    Gateway --> Identity & Market & Portfolio & Notifications

    Services <-->|Event Bus| Bus
    Services <-->|Cache| Cache
    Services <-->|Persistence| DBs

    classDef client fill:#f8fafc,stroke:#64748b,stroke-width:1.5px,color:#1e293b,rx:8
    classDef gw fill:#eff6ff,stroke:#3b82f6,stroke-width:2px,color:#1d4ed8,rx:8
    classDef svc fill:#f0fdf4,stroke:#16a34a,stroke-width:1.5px,color:#14532d,rx:8
    classDef infra fill:#fffbeb,stroke:#d97706,stroke-width:1.5px,color:#78350f,rx:8

    class Web,Mobile client
    class Gateway gw
    class Identity,Market,Portfolio,Notifications svc
    class Bus,Cache,DBs infra
```

*For detailed specifications, queue bindings, and container mappings, see [Containers Architecture (C4 Level 2)](docs/architecture/containers.md).*

---

## Technology Stack

- **Platform:** .NET 9.0 (C# 13)
- **API Gateway:** YARP (Yet Another Reverse Proxy)
- **Message Broker:** RabbitMQ 3 with MassTransit
- **Databases:** PostgreSQL 16+ via EF Core (Npgsql)
- **Distributed Cache:** Redis 6.2
- **Real-Time Push:** ASP.NET Core SignalR
- **Observability:** OpenTelemetry .NET SDK + Jaeger
- **External AI:** Google Gemini REST API

---

## Quick Start

Launch the entire system (all 5 services, PostgreSQL, Redis, RabbitMQ, and Jaeger) using Docker Compose:

```bash
git clone https://github.com/ibrahimkabadayi/Identity.API.git
cd Identity.API
cp .env.example .env
docker compose up -d --build
```

The API Gateway is immediately available at `http://localhost:5000`.

*For local debugging, IDE configuration, database update commands, and troubleshooting, see the [Running Locally Guide](docs/operations/running-locally.md).*

---

## Documentation

Full architectural specifications, database models, decision logs, and API reference documentation live in the [`docs/`](docs/) directory:

- **[Documentation Index](docs/README.md)** — Start here for a map of all documentation.
- **[Architecture](docs/architecture/)** — [System Context (C4 L1)](docs/architecture/context.md), [Containers (C4 L2)](docs/architecture/containers.md), [Deployment](docs/architecture/deployment.md), and [Messaging Topology](docs/architecture/messaging-topology.md).
- **[Database](docs/database/)** — Per-service ERDs, cross-service logical relationships, and Redis keys catalog.
- **[Domain & Sequence Flows](docs/flows/)** — Step-by-step sequence diagrams for trading, limit orders, and transfers.
- **[Event Catalog](docs/events.md)** — Message contracts, publisher and subscriber matrix.
- **[Architecture Decisions (ADRs)](docs/decisions/)** — Architectural decision records including database per service and MongoDB to PostgreSQL migration.
- **[API Reference](docs/api/)** — Endpoint reference tables and OpenAPI specs.
- **[Operations](docs/operations/)** — Local environment setup, glossary, and system limitations.
- **[Contributing Guide](docs/CONTRIBUTING-DOCS.md)** — Documentation conventions, Mermaid guidelines, and PR quality checklist.

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
