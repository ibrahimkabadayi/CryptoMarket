# Owner Notes for ADRs

> **For the project owner.** Fill in the sections below so the agent can write proper ADRs.
> Bullets are enough — the agent will expand them into full ADR format.
> Leave sections blank if you're unsure; the agent will mark them as `TODO(owner)`.

---

## ADR 0001 / 0002 — Databases

- What problem pushed you away from MongoDB in MarketDb?
- What did PostgreSQL give you instead (relational integrity, transactions, querying, tooling, consistency with other services)?
- Was anything lost or hard in the migration?
- Why one database per service rather than a shared one?

**Your notes:**

> _TODO(owner): please fill in_

---

## ADR 0003 — RabbitMQ

- Which alternatives did you consider (Kafka, others)?
- What decided it (simplicity, MassTransit support, scale needs, learning goals)?
- What would make you reconsider?

**Your notes:**

> _TODO(owner): please fill in_

---

## ADR 0004 — Event-based vs HTTP

- Which interactions are event-based and why?
- What failure/coupling problems of synchronous calls were you avoiding?
- What new problems did events introduce (duplicates, ordering, eventual consistency) and how are they handled?

**Your notes:**

> _TODO(owner): please fill in_

---

## ADR 0005 — Middleware

- For each service, why does each middleware exist and why in that order?

**Your notes:**

> _TODO(owner): please fill in_

---

## ADR 0006 — Redis

- What is Redis used for?
- Why not `IMemoryCache`? (multiple instances, persistence, sharing between services, TTL needs…)

**Your notes:**

> _TODO(owner): please fill in_

---

## ADR 0007 — JWT Identity

- What risk or bug motivated this?
- Which endpoints/consumers were affected?

**Your notes:**

> _TODO(owner): please fill in_
