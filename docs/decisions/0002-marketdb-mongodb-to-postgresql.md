# 0002 - MarketDb: migrating from MongoDB to PostgreSQL

- Status: Accepted
- Date: 2026-07-10 (recorded retroactively)

## Context

Originally, `Market.API` utilized MongoDB as its persistent datastore for crypto coin definitions, market prices, and historical time-series data. The remainder of the backend microservices (`Identity.API`, `Portfolio.API`, `Notifications.API`) operated on relational PostgreSQL databases via Entity Framework Core.

## Decision

Migrate `Market.API` from MongoDB to PostgreSQL (`MarketDb`), aligning its infrastructure with the rest of the ecosystem on Entity Framework Core and Npgsql. Read performance requirements previously satisfied by MongoDB are shifted to an in-memory Redis caching layer (`market:coins`).

## Alternatives considered

- **Retaining MongoDB:** Maintaining MongoDB as a document store specifically for market data and price histories.

## Consequences

- (+) **Operational and Code Simplicity:** Eliminates the cognitive friction and disparate repository patterns required by MongoDB NoSQL drivers. The entire backend now uses a unified EF Core repository and DbContext paradigm.
- (+) **Tooling Ergonomics:** PostgreSQL seamlessly integrates with database IDEs like DataGrip, whereas MongoDB could previously only be inspected via command-line console in the owner's environment (cause unknown).
- (+) **Relational Schema Constraints:** Introduces schema enforcement and composite unique indexes (e.g., `(Symbol, Timestamp)` in `PriceHistories`).
- (-) **Data Loss During Migration:** Existing historical market records and seeded documents were wiped during the transition (deemed acceptable by owner due to developmental phase).
- (-) **Residual Configuration:** A dangling `mongo_data` volume declaration remains in `docker-compose.yml`.

## Evidence

The migration was performed across five contiguous git commits on 2026-07-10:
1. `4e73f7a` (11:46:59): Removed `MarketContext.cs` and `MongoDbSettings.cs`.
2. `a2743c5` (11:47:06): Stripped MongoDB attributes (`[BsonId]`, `[BsonRepresentation]`) from domain entities (`BaseEntity.cs`, `Coin.cs`).
3. `64159d5` (11:47:13): Refactored `CoinRepository.cs` from Mongo collections to EF Core methods.
4. `ee37e99` (11:47:20): Registered `ApplicationDbContext` with Npgsql in `DependencyInjection.cs`.
5. `f92b992` (11:47:28): Replaced MongoDB container in `docker-compose.yml` with PostgreSQL configuration and updated `appsettings.json`.
- Residual artifact: [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L159) line 159 still declares volume `mongo_data:`.

## Rationale source

- [_owner-notes.md](file:///c:/CryptoMarketProject/backend/docs/decisions/_owner-notes.md#L17-L25) — Section 0002.

---

## Sources

- Git commits: `4e73f7a`, `a2743c5`, `64159d5`, `ee37e99`, `f92b992`
- [docker-compose.yml](file:///c:/CryptoMarketProject/backend/docker-compose.yml#L159)
- [Market.API/Infrastructure/Context/ApplicationDbContext.cs](file:///c:/CryptoMarketProject/backend/Market.API/Infrastructure/Context/ApplicationDbContext.cs)
