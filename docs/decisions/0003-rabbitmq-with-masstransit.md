# 0003 - RabbitMQ (with MassTransit) instead of Kafka or other message brokers

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

Microservices in CryptoMarket require asynchronous, decoupled communication to propagate domain events such as user creations, price updates, order placement, and trade notifications without tight temporal coupling. A messaging broker and abstraction layer had to be selected.

## Decision

Use **RabbitMQ** orchestrated through the **MassTransit** abstraction framework for .NET.

## Alternatives considered

- **Apache Kafka:** A distributed event streaming platform evaluated for high-throughput partitioned logs.
- **Other Message Brokers:** Cloud-specific brokers (Azure Service Bus, AWS SQS) or bare AMQP clients.
- **Deciding Factor:** Learning curve only. RabbitMQ offered the simplest mental model, quick local development via Docker, and robust turnkey management UI.

## Consequences

- (+) **Developer Ergonomics & Learning Curve:** Minimal operational overhead; easy to inspect exchanges, queues, and consumers via RabbitMQ Management UI (`http://localhost:15672`).
- (+) **MassTransit Abstraction:** Decouples domain consumer code from raw AMQP client mechanics. Switching or extending transport providers in the future requires minimal changes to consumer logic.
- (-) **Reconsideration Trigger:** If the architecture evolves to require high-fanout stream replay or continuous event distribution to a very large number of concurrent consumers, Kafka would be reconsidered. Current workload does not justify the added operational complexity.

## Evidence

- **Broker Orchestration:** [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L134-L148) deploys `rabbitmq:3-management` on ports `5672` (AMQP) and `15672` (Management UI) with health checks (`rabbitmq-diagnostics -q ping`).
- **MassTransit Registration:** Every microservice configures MassTransit using RabbitMQ in `Program.cs`:
  - `Identity.API/Program.cs` (`AddMassTransit` with `UsingRabbitMq`)
  - `Market.API/Program.cs` (`AddMassTransit` with `UsingRabbitMq`)
  - `Notifications.API/Program.cs` (`AddMassTransit` with consumers on explicit receive endpoints)
  - `Portfolio.API/Program.cs` (`AddMassTransit` with consumers on explicit receive endpoints)
- **Shared Contracts:** All message schemas are isolated in the `Shared.Messages` class library.

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L26-L33) — Section 0003.

---

## Sources

- [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L134-L148)
- [Identity.API/Program.cs](file:///c:/CryptoMarketProject/backend/Identity.API/Program.cs)
- [Market.API/Program.cs](file:///c:/CryptoMarketProject/backend/Market.API/Program.cs)
- [Notifications.API/Program.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Program.cs)
- [Portfolio.API/Program.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Program.cs)
- [Shared.Messages](file:///c:/CryptoMarketProject/backend/Shared.Messages/)
