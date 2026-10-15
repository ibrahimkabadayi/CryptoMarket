# Architecture Decision Records

> Index of all Architecture Decision Records (ADRs) for the CryptoMarket project.

---

## ADR Index

| No. | Title | Status | Date |
|---|---|---|---|
| 0001 | [Database per service with PostgreSQL](0001-database-per-service.md) | _TBD_ | _TBD_ |
| 0002 | [MarketDb: migrating from MongoDB to PostgreSQL](0002-marketdb-mongodb-to-postgresql.md) | _TBD_ | _TBD_ |
| 0003 | [RabbitMQ (with MassTransit) instead of Kafka or other message brokers](0003-rabbitmq-with-masstransit.md) | _TBD_ | _TBD_ |
| 0004 | [Event-based communication instead of synchronous HTTP between services](0004-event-based-communication.md) | _TBD_ | _TBD_ |
| 0005 | [Per-service middleware pipelines: purpose and rationale](0005-per-service-middleware.md) | _TBD_ | _TBD_ |
| 0006 | [Redis instead of in-memory caching](0006-redis-caching.md) | _TBD_ | _TBD_ |
| 0007 | [User identity is always derived from the JWT, never from the client](0007-jwt-identity.md) | _TBD_ | _TBD_ |

---

## Process

1. The owner provides rationale and context in [`_owner-notes.md`](_owner-notes.md).
2. The agent extracts code/git evidence and drafts each ADR.
3. Gaps are marked with `TODO(owner)` — the agent never guesses the "why".

See [CONTRIBUTING-DOCS.md](../CONTRIBUTING-DOCS.md) for the full operating rules.

---

## Sources

- `CRYPTOMARKET_DOCS_IMPLEMENTATION_PLAN.md` — Phase 7
