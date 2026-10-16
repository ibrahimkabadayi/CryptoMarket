# 0004 - Event-based communication instead of synchronous HTTP between services

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

In a distributed microservice architecture, services often need to trigger side effects across service boundaries (e.g., creating a wallet when a user registers, matching orders when prices tick, notifying users when trades occur). These operations can be executed via synchronous HTTP RPC or asynchronous event messaging.

## Decision

Adopt asynchronous, event-driven communication via RabbitMQ and MassTransit for all inter-service interactions. Direct service-to-service synchronous HTTP calls are prohibited and non-existent in the codebase.

## Alternatives considered

- **Direct Synchronous HTTP REST / gRPC:** Invoking downstream microservices synchronously during HTTP request handling.

## Consequences

- (+) **Temporal Decoupling:** Downstream service downtime or latency does not block upstream callers or cause cascading failures.
- (+) **Zero Direct HTTP Dependencies:** Services operate independently; each service publishes events without knowing the identity or count of downstream consumers.
- (-) **Duplicate Delivery Risk:** At-least-once delivery semantics in RabbitMQ mean consumers can receive duplicate events. Currently, deduplication via Redis `CorrelationId` caching is implemented **only** for `LimitOrderPlacedEvent` in `Portfolio.API` (`SetLimitOrderEvent` / `LimitOrderService.CreateLimitOrderAsync`). Other consumers lack deduplication guards.
- (-) **Eventual Consistency:** State updates across boundaries are eventual. The owner notes that consistency lag has **not been observed as a problem so far**, likely due to low development-level traffic volume.
- (-) **Failure After Caller Receives 200 OK:** If an event processing error occurs downstream after an upstream API call returns success, the operation fails silently from the caller's immediate perspective. MassTransit routes the faulted message to a `<queue>_error` queue. The owner has **not observed** this scenario in practice, and no automated compensation or saga rollback is implemented.
- (-) **Message Ordering:** The owner has **not observed** out-of-order message delivery issues; no partitioning or sequential message ordering mechanisms are configured.

## Evidence

- **Direct HTTP Calls:** A repository-wide audit confirms **0 synchronous HTTP calls** exist between internal services (see [System Inventory](file:///c:/CryptoMarketProject/backend/docs/_inventory.md#L193-L200)).
- **Event Catalog:** All inter-service message contracts are maintained in [docs/events.md](file:///c:/CryptoMarketProject/backend/docs/events.md).
- **Deduplication Audit:** Only `LimitOrderService.CreateLimitOrderAsync` validates `cacheService.GetAsync<string>(correlationId.ToString())` with a 24-hour TTL before inserting to `PortfolioDb`. All other consumers lack deduplication checks.
- **Fault Handling:** Standard MassTransit behavior routes faulted messages to dead-letter error queues (`notification-user-created-queue_error`, etc.). No Transactional Outbox pattern (`AddEntityFrameworkOutbox`) is configured.

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L34-L50) — Section 0004.

---

## Sources

- [docs/_inventory.md](file:///c:/CryptoMarketProject/backend/docs/_inventory.md#L106-L135)
- [docs/events.md](file:///c:/CryptoMarketProject/backend/docs/events.md)
- [Portfolio.API/Application/Services/LimitOrderService.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Services/LimitOrderService.cs#L69-L98)
- [Shared.Messages](file:///c:/CryptoMarketProject/backend/Shared.Messages/)
