# Running Locally

> Complete guide for setting up, running, debugging, and troubleshooting the CryptoMarket platform on a local workstation.

---

## 1. Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (with Linux containers enabled)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) (18.x), JetBrains Rider, or VS Code with C# Dev Kit
- Optional: [PostgreSQL Client / DataGrip](https://www.jetbrains.com/datagrip/) for inspecting databases

---

## 2. Quick Start: Full Stack via Docker Compose

This is the fastest way to run the entire backend stack including all 5 services and 4 infrastructure containers.

```bash
# 1. Clone repository
git clone https://github.com/ibrahimkabadayi/Identity.API.git
cd Identity.API

# 2. Configure environment file
cp .env.example .env
# Edit .env and supply required values (names listed in Section 5)

# 3. Launch full stack
docker compose up -d --build
```

Startup takes approximately 60–90 seconds on first run while images build and RabbitMQ initializes. Services wait for RabbitMQ health check before starting.

The API Gateway is reachable at `http://localhost:5000`.

---

## 3. Alternative: Running Individual Services with `dotnet run`

To iterate quickly with breakpoints on a single service while running backing infrastructure in Docker:

### Step 3.1: Start Backing Infrastructure Only

```bash
docker compose up -d postgres.db redis rabbitmq jaeger
```

### Step 3.2: Run Services Individually

Each service has an `appsettings.Development.json` file configuring connection strings for `localhost`:

```bash
# Identity.API (Port 5145 HTTP / 7081 HTTPS)
dotnet run --project Identity.API/Identity.API.csproj

# Market.API (Port 5226 HTTP / 7080 HTTPS)
dotnet run --project Market.API/Market.API.csproj

# Portfolio.API (Port 5252 HTTP / 7110 HTTPS)
dotnet run --project Portfolio.API/Portfolio.API.csproj

# Notifications.API (Port 5291 HTTP / 7029 HTTPS)
dotnet run --project Notifications.API/Notifications.API.csproj

# API.Gateway (Port 5161 HTTP / 7049 HTTPS)
dotnet run --project API.Gateway/API.Gateway.csproj
```

---

## 4. Port Mappings Reference

### Docker Compose Ports (Production / Full Stack)

Internal containers listen on port `8080`. Host ports are mapped as follows:

| Service / Component | Host Port | Container Port | Purpose / Access URL |
|---|---|---|---|
| **API Gateway** | `5000` | `8080` | `http://localhost:5000` — Public reverse proxy entry point |
| **Identity.API** | `8001` | `8080` | `http://localhost:8001` — Direct service access |
| **Market.API** | `8002` | `8080` | `http://localhost:8002` — Direct service access |
| **Portfolio.API** | `7000` | `8080` | `http://localhost:7000` — Direct service access |
| **Notifications.API** | `6000` | `8080` | `http://localhost:6000` — Direct service access |
| **PostgreSQL** | `5432` | `5432` | `localhost:5432` — Database server hosting all 4 DBs |
| **Redis** | `6379` | `6379` | `localhost:6379` — Distributed cache & idempotency store |
| **RabbitMQ (AMQP)** | `5672` | `5672` | `localhost:5672` — MassTransit event broker |
| **RabbitMQ (Management)** | `15672` | `15672` | `http://localhost:15672` — RabbitMQ web dashboard |
| **Jaeger (UI)** | `16686` | `16686` | `http://localhost:16686` — Distributed tracing dashboard |
| **Jaeger (OTLP gRPC)** | `4317` | `4317` | `localhost:4317` — OpenTelemetry trace collector |

### Local `dotnet run` Launch Ports

When launched locally outside Docker via `launchSettings.json`:

| Service | Local HTTP | Local HTTPS |
|---|---|---|
| Identity.API | `http://localhost:5145` | `https://localhost:7081` |
| Market.API | `http://localhost:5226` | `https://localhost:7080` |
| Portfolio.API | `http://localhost:5252` | `https://localhost:7110` |
| Notifications.API | `http://localhost:5291` | `https://localhost:7029` |
| API.Gateway | `http://localhost:5161` | `https://localhost:7049` |

---

## 5. Configuration & Environment Variables

All secrets and runtime variables are loaded from the root `.env` file (copied from [`.env.example`](../../.env.example)).

> [!IMPORTANT]
> Never commit actual passwords, API keys, or connection strings into version control.

### Required Environment Variable Names

| Variable Name | Used By | Purpose |
|---|---|---|
| `RABBITMQ_USER` | All API services | RabbitMQ authentication username |
| `RABBITMQ_PASS` | All API services | RabbitMQ authentication password |
| `JWT_SECRET_KEY` | All API services | Symmetric signing key for HS256 JWT tokens (min 32 characters) |
| `AI_KEY` | Market.API | Google Gemini API key for synthetic crypto news generation |
| `EMAIL_PASSWORD` | Notifications.API | SMTP password for outbound notification emails |
| `POSTGRES_USERNAME` | postgres.db | PostgreSQL administrative username |
| `POSTGRES_PASSWORD` | postgres.db | PostgreSQL administrative password |
| `REDIS_USERNAME` | redis | Optional Redis ACL username |
| `REDIS_PASSWORD` | redis | Optional Redis ACL password |
| `DOCKER_REGISTRY` | docker-compose | Optional image name prefix |

### Connection String Patterns

In `docker-compose.yml`, connection strings point to the container DNS name `postgres.db`:
- `ConnectionStrings:DefaultConnection = Host=postgres.db;Port=5432;Database=<DatabaseName>;Username=...;Password=...`

Database names:
- `IdentityDb` (Identity.API)
- `MarketDb` (Market.API)
- `PortfolioDb` (Portfolio.API)
- `NotificationsDb` (Notifications.API)

---

## 6. Database Migrations

Each persistent service manages its own migrations through Entity Framework Core and Npgsql. Migrations are **not** run automatically on startup.

### Applying Migrations to PostgreSQL

Ensure `postgres.db` is running first:

```bash
# Identity.API
dotnet ef database update --project Identity.API/Identity.API.csproj --startup-project Identity.API/Identity.API.csproj --context ApplicationDbContext

# Market.API
dotnet ef database update --project Market.API/Market.API.csproj --startup-project Market.API/Market.API.csproj --context ApplicationDbContext

# Portfolio.API
dotnet ef database update --project Portfolio.API/Portfolio.API.csproj --startup-project Portfolio.API/Portfolio.API.csproj --context ApplicationDbContext

# Notifications.API
dotnet ef database update --project Notifications.API/Notifications.API.csproj --startup-project Notifications.API/Notifications.API.csproj --context ApplicationDbContext
```

### Adding New Migrations

```bash
dotnet ef migrations add <MigrationName> --project <Service>/<Service>.csproj --startup-project <Service>/<Service>.csproj --context ApplicationDbContext
```

---

## 7. Health Checks & Observability

### Health Endpoints

Each microservice exposes standardized ASP.NET Core health checks:

- **Liveness Probe:** `GET /health/live` — Returns `200 Healthy` if the process is alive.
- **Readiness Probe:** `GET /health/ready` — Checks connectivity to PostgreSQL, Redis, and RabbitMQ.

```bash
# Test Identity health
curl http://localhost:8001/health/live
curl http://localhost:8001/health/ready
```

### Distributed Tracing (Jaeger)

All services use OpenTelemetry with automatic ASP.NET Core, HttpClient, and MassTransit instrumentation.

1. Navigate to [`http://localhost:16686`](http://localhost:16686) in your browser.
2. Select any service from the **Service** dropdown (e.g. `Portfolio.API` or `API.Gateway`).
3. Click **Find Traces** to view end-to-end request latency, spans, and RabbitMQ event hops.

---

## 8. Running Automated Tests

The solution includes a test suite in `tests/CryptoMarket.UnitTests` covering business logic across all microservices using xUnit, NSubstitute, and FluentAssertions.

```bash
# Run unit tests directly
dotnet test tests/CryptoMarket.UnitTests/CryptoMarket.UnitTests.csproj
```

---

## 9. Troubleshooting & Common Issues

| Issue | Likely Cause | Solution |
|---|---|---|
| **Services exit immediately on startup** | Missing or incomplete `.env` file | Ensure `.env` exists in the repository root and contains valid variable entries. |
| **`Connection refused` on RabbitMQ** | RabbitMQ container still initializing | Services use `condition: service_healthy` in compose, but if starting manually, wait 10-15s until RabbitMQ is responsive. |
| **Database table does not exist** | Migrations have not been applied | Run `dotnet ef database update` for the affected service as described in Section 6. |
| **Token rejected across services** | `JWT_SECRET_KEY` differs between services | Check `.env` to verify all services read the same shared signing key. |
| **SignalR connection dropped** | Token missing from query string | SignalR WebSockets must pass JWT in the query parameter: `?access_token=<jwt>`. |
| **Port conflict on 5000 / 5432 / 6379** | Another local service running on the same port | Stop conflicting local PostgreSQL/Redis instances or modify host ports in `docker-compose.yml`. |

### Inspecting Container Logs

```bash
# View live logs for all containers
docker compose logs -f

# View logs for a specific service
docker compose logs -f portfolio.api
docker compose logs -f market.api
docker compose logs -f rabbitmq
```

### Resetting Persistent Volumes

```bash
# Wipe all container data (PostgreSQL, Redis, RabbitMQ)
docker compose down -v
```

---

## Sources

- `README.md` — Sections: Local Development, Docker & Compose Details, Environment Variables, Health Checks, Observability, Troubleshooting
- `docker-compose.yml`
- `docker-compose.override.yml`
- `.env.example`
- `docs/_inventory.md`
