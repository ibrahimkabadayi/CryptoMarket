# Architecture Documentation

> Architecture documentation for the CryptoMarket microservices platform based on the C4 model.

---

## Architecture Documents

| Document | Level / Scope | Description |
|---|---|---|
| [System Context (C4 Level 1)](context.md) | High-Level System Context | Platform boundary, human actors (traders), and external integrations (Google Gemini AI, SMTP server). |
| [Containers (C4 Level 2)](containers.md) | Container & Service Level | High-level container map: API Gateway (YARP), 4 microservices, PostgreSQL databases, Redis caching, and RabbitMQ event bus. |
| [Deployment](deployment.md) | Physical Infrastructure | Docker Compose container deployment, port allocations, networks, volumes, and health checks. |
| [Messaging Topology](messaging-topology.md) | Event Topology | RabbitMQ exchanges, message routing, queues, consumers, and fault handling. |
| [Middleware](middleware.md) | Pipeline Mechanics | Per-service HTTP middleware pipeline registration order and cross-cutting concerns. |
| [Security](security.md) | Identity & Auth | JWT token lifecycle, claim propagation, and endpoint authorization matrix. |

---

## Sources

- `CRYPTOMARKET_DOCS_IMPLEMENTATION_PLAN.md` — Section 2 (Target structure)
- `docs/_inventory.md`
