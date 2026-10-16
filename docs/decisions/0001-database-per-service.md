# 0001 - Database per service with PostgreSQL

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

The CryptoMarket backend is composed of multiple microservices: `Identity.API`, `Market.API`, `Portfolio.API`, and `Notifications.API`. When architecting data storage, a decision was required regarding whether services should share a centralized database schema or retain isolated data stores.

## Decision

Each microservice possesses and manages its own dedicated PostgreSQL database:
- `IdentityDb` (owned by `Identity.API`)
- `MarketDb` (owned by `Market.API`)
- `NotificationsDb` (owned by `Notifications.API`)
- `PortfolioDb` (owned by `Portfolio.API`)

The services run against a PostgreSQL server instance, but each maintains separate schemas, migrations, and connections.

## Alternatives considered

- **Shared Central Database:** A single PostgreSQL database accessed by all microservices with cross-table foreign key constraints.

## Consequences

- (+) **Fault Isolation:** Keeps each service's data independent; if one database or service experiences an outage or lock contention, the remaining services are not affected.
- (+) **Independent Schema Evolution:** Each service team/module can modify, migrate, or deploy its database independently via Entity Framework Core without coordinating locks or breaking other services.
- (-) **No Physical Foreign Keys:** Relational integrity cannot be enforced at the database level across service boundaries. Cross-service entity identifiers (such as `UserId` in `Wallets`) are logical references without foreign key constraints.
- (-) **Eventual Consistency:** Inter-service data coordination must be achieved asynchronously through message events (RabbitMQ), requiring eventual consistency handling.

## Evidence

- **Database Configuration:**
  - `Identity.API/appsettings.json` points to database `IdentityDb`
  - `Market.API/appsettings.json` points to database `MarketDb`
  - `Notifications.API/appsettings.json` points to database `NotificationsDb`
  - `Portfolio.API/appsettings.json` points to database `PortfolioDb`
- **Docker Compose Setup:** [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L106-L121) provisions `postgres.db` with separate databases.
- **Cross-Service References:** Documented comprehensively in [logical-relationships.md](file:///c:/CryptoMarketProject/backend/docs/database/logical-relationships.md), confirming zero physical cross-service foreign keys.

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L9-L16) — Section 0001.

---

## Sources

- [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L106-L121)
- [Identity.API/appsettings.json](file:///c:/CryptoMarketProject/backend/Identity.API/appsettings.json)
- [Market.API/appsettings.json](file:///c:/CryptoMarketProject/backend/Market.API/appsettings.json)
- [Notifications.API/appsettings.json](file:///c:/CryptoMarketProject/backend/Notifications.API/appsettings.json)
- [Portfolio.API/appsettings.json](file:///c:/CryptoMarketProject/backend/Portfolio.API/appsettings.json)
- [logical-relationships.md](file:///c:/CryptoMarketProject/backend/docs/database/logical-relationships.md)
