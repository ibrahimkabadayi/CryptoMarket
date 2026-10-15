# ADR Owner Notes

> Input file for Phase 7. Place at `docs/decisions/_owner-notes.md`.
> Content below is the owner's own wording, lightly structured. The agent must not add reasons that are not written here. Anything marked `TODO(owner)` is still unanswered; keep it as `TODO(owner)` in the resulting ADR.
> "Agent evidence tasks" are things the agent can determine from code, config, or git history; they are facts, not rationale.

---

## 0001 — Database per service with PostgreSQL

- Decision: each service has its own PostgreSQL database.
- Rationale: keeps each service's data **independent**, and ensures that when one database or one service goes down, the others are **not affected**.
- Agent evidence tasks:
  - Confirm from `docker-compose.yml` and connection configuration that each service uses its own database (names only, no credentials).
  - Cross-service references are logical, not FK-enforced; link to `docs/database/logical-relationships.md` under Consequences.

## 0002 — MarketDb: migrating from MongoDB to PostgreSQL

- Original choice: MongoDB for MarketDb. Later migrated to PostgreSQL.
- Reason for migrating: **simplicity**. MongoDB, being NoSQL, required a different implementation style in code, and the owner is used to SQL, so it was unnecessary friction.
- Trade-off accepted: the database read-speed advantage of the previous setup was handled with **Redis** instead.
- Additional reason: MongoDB could only be accessed from the console; the owner could not connect to it from **DataGrip** (cause unknown). Record the cause as unknown; do not speculate.
- Consequences (-): the existing **market data was lost** during the migration, but this caused little trouble.
- Agent evidence tasks: git history of the migration (commits, dates, files changed).

## 0003 — RabbitMQ (with MassTransit) instead of Kafka or other message brokers

- Decision: RabbitMQ.
- Reason: it has the **easiest learning curve**.
- Because **MassTransit** is used, the choice of broker makes little difference in application code.
- Alternatives named: Kafka and other message brokers. Stated deciding factor: learning curve only.
- Reconsider when: the same message has to be delivered continuously to **many consumers**, in which case the owner would use Kafka. The project has no such requirement today, so there is no plan to switch.

## 0004 — Event-based communication instead of synchronous HTTP between services

- Decision: event-based, asynchronous communication between services.
- Rationale: make sure **messages are not lost** and communicate **asynchronously**, so that one service does not depend on another.
- Problems introduced by event-based communication and how they are handled (owner's statements):
  - **Duplicate delivery:** handled by storing the `CorrelationId` in the cache and checking it before processing. The owner put considerable effort into this. It has **not been applied to every event/consumer**.
  - **Eventual consistency (result not visible immediately):** not a problem so far. The owner believes this is likely because the system has never seen much traffic, so it has not been observed under load.
  - **Failure after the caller already got a response:** the owner has **not seen** this happen. No explicit handling is claimed.
  - **Message ordering:** the owner has **not seen** this happen. No explicit handling is claimed.
- Record "not observed" as exactly that. Do not write it as "handled" or "solved".
- Agent evidence tasks:
  - Document the actual RabbitMQ/MassTransit durability and retry configuration (durable queues, persistent messages, retry policy, error queues, outbox/inbox if any). Do not claim "no message loss" beyond what the configuration supports; state what is configured.
  - List which interactions are event-based and which (if any) are synchronous HTTP.
  - Document existing duplicate-handling mechanisms found in consumers (separate from the HTTP `IdempotencyMiddleware`).
  - Produce a table of **every consumer** with a column "has duplicate-message check (cache by `CorrelationId`)? yes/no", plus how the check works (when the key is written relative to processing, TTL). Put it in `docs/events.md` and reference it from the ADR. Consumers without the check go to `known-limitations.md` as a neutral fact.
  - Document the retry and error-queue behavior actually configured for consumers, so the "failure after the caller got a response" path is described as it really behaves (even if the owner has never observed it).

## 0005 — Per-service middleware pipelines

| Service | Middleware | Purpose (owner's description) |
|---|---|---|
| Identity | `RateLimitingMiddleware` | Prevents sending more than a set number of requests to the service within a short time. Security against attacks. |
| Portfolio | `IdempotencyMiddleware` | Prevents the same request from being executed again. Prevents wrong or duplicate buy/sell operations. |
| All services | `ExceptionHandlingMiddleware` | Wraps every request in a try/catch block. |
| All services | `LoggingMiddleware` | Logs every HTTP request with its status code and duration. |
| All services | `CorrelationIdMiddleware` | Ensures each request carries a unique identifier from start to finish; valuable for logging. |

- Confirmed by owner: **rate limiting exists only in the Identity service.**
- Registration order per service: the agent extracts this from each `Program.cs`; owner reviews.
- Notes for the agent:
  - `IdempotencyMiddleware` (HTTP level) and any message-consumer idempotency (e.g., correlation-id checks in consumers) are **separate mechanisms**. Document each from the code and do not merge them.
  - Record in `known-limitations.md`, as a neutral fact, that rate limiting is configured only in Identity (and that the other services have none), plus which services lack idempotency middleware, if the code confirms it. Do not judge or recommend.

## 0006 — Redis instead of in-memory caching

- Decision: Redis.
- Rationale: cached data should remain available for its configured lifetime. If a service goes down, its `IMemoryCache` is lost with it, whereas Redis keeps the data.
- Intended main benefit: a **cache shared between services**. **Owner states this is not currently exercised in this project** (no key is shared across services). Record it as an intended benefit that is not currently used, not as a current feature.
- Also: Redis is what covers database read speed after leaving MongoDB (see 0002).
- Agent evidence tasks:
  - List every Redis key pattern with writer and reader service (`docs/database/redis-keys.md`) and confirm whether any key is actually shared across services.
  - Document the Redis persistence configuration from `docker-compose.yml` (volume, append-only/snapshot settings, or none). State what survives a Redis restart as configured; do not assume it.

## 0007 — User identity is always derived from the JWT, never from the client

- Decision: `userId` comes from the JWT claims only; the change is implemented.
- Rationale: when `userId` was taken from the route, anyone could act on behalf of another person by typing that person's correct `userId`. (The caller must already be authenticated to perform any operation.)
- Where JWT-derived identity is used today (owner's statement): **LimitOrder**, **all of Wallet**, **some** Notification endpoints, **some** PriceAlert endpoints, **some** Identity endpoints, and **some** Market endpoints.
- Owner's clarification: `userId` is **always** taken from the JWT wherever an endpoint is user-scoped. The endpoints that do not use it are the ones that **do not require authentication** (anonymous endpoints). The ADR title "always" stands, subject to the agent's verification below.
- Agent evidence tasks:
  - From git history and current code, list which endpoints and consumers were affected by the change and how the identity now reaches them.
  - Classify **every** endpoint in every service into exactly one of: (a) uses JWT-derived identity, (b) anonymous or not user-scoped (e.g., public market data), (c) user-scoped but identity comes from somewhere other than the JWT. Put the table in `docs/api/endpoints.md` or `docs/architecture/security.md`.
  - The owner expects no endpoint in (c). Verify this from the code. If the table confirms it, state in the ADR that the classification was verified at commit `<hash>`. If any endpoint falls in (c), do not change the ADR claim silently: list it in `known-limitations.md` as a neutral fact and report it in the PR description.
