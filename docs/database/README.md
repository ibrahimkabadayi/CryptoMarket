# Database Overview

> Architecture, schema mappings, migration approaches, and decision records for all CryptoMarket databases.

---

## Service → Database Mapping

The platform strictly implements the **Database-per-Service** pattern. Each microservice connects exclusively to its own dedicated PostgreSQL database, with zero cross-database queries or shared connections.

| Service | Database Name | ORM & Provider | Migration Approach | Curated Schema Doc | Raw DataGrip Export |
|---|---|---|---|---|---|
| **Identity.API** | `IdentityDb` | EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) | Code-First (`Identity.API/Migrations/`), managed via `dotnet ef` | [identity-db.md](identity-db.md) | [identity.mmd](generated/identity.mmd) |
| **Market.API** | `MarketDb` | EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) | Code-First (`Market.API/Migrations/`), managed via `dotnet ef` | [market-db.md](market-db.md) | [market.mmd](generated/market.mmd) |
| **Portfolio.API** | `PortfolioDb` | EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) | Code-First (`Portfolio.API/Migrations/`), managed via `dotnet ef` | [portfolio-db.md](portfolio-db.md) | [portfolio.mmd](generated/portfolio.mmd) |
| **Notifications.API** | `NotificationsDb` | EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) | Code-First (`Notifications.API/Migrations/`), managed via `dotnet ef` | [notifications-db.md](notifications-db.md) | [notifications.mmd](generated/notifications.mmd) |
| **API.Gateway** | *(None)* | N/A | Stateless reverse proxy (YARP) | N/A | N/A |

---

## Cross-Database Relationships & Caching

- **Logical Relationships:** Because databases are physically separated, relationships between services (such as user references in wallets and orders, or coin symbols in holdings) are logical only. See [Cross-Service Logical Relationships](logical-relationships.md) for the complete relationship matrix and overview diagram.
- **In-Memory & Distributed Caching:** Fast read paths and request idempotency are backed by Redis. See the [Redis Key Catalog](redis-keys.md) for key namespaces, TTLs, and persistence details.

---

## Architecture Decisions

The architectural principles governing data persistence in CryptoMarket are documented in the following Architecture Decision Records:

- [ADR 0001 — Database per service with PostgreSQL](../decisions/0001-database-per-service.md)
- [ADR 0002 — MarketDb: migrating from MongoDB to PostgreSQL](../decisions/0002-marketdb-mongodb-to-postgresql.md)

---

## Sources

- `Identity.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Market.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Portfolio.API/Infrastructure/Context/ApplicationDbContext.cs`
- `Notifications.API/Infrastructure/Context/ApplicationDbContext.cs`
- `docker-compose.yml`
- `docs/_inventory.md` — Section 2 (Databases per service)
- `docs/decisions/_owner-notes.md` — ADR 0001 & ADR 0002
