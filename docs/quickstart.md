# Local Development Quickstart Guide

This guide walks you through bootstrapping the **FreshCart** microservices backend on your local development machine using Docker Compose and the .NET CLI.

---

## 1. Prerequisites

Ensure you have the following installed on your workstation:
- **.NET SDK** (9.0 or 10.0-preview) &mdash; [Download .NET](https://dotnet.microsoft.com/download)
- **Docker Desktop** (or Docker Engine with Compose v2) &mdash; [Get Docker](https://www.docker.com/products/docker-desktop)
- **Git**

Verify prerequisites in your terminal:
```bash
dotnet --version
docker compose version
```

---

## 2. Clone the Repository

```bash
git clone https://github.com/amasen02/freshcart-backend.git
cd freshcart-backend
```

---

## 3. Launch Backing Services (Infrastructure Stack)

FreshCart relies on PostgreSQL, Redis, RabbitMQ, and OpenTelemetry collector for event-driven messaging, caching, and observability. Launch them via the provided Docker Compose stack:

```bash
docker compose -f deploy/docker/docker-compose.yaml up -d
```

### Checking Container Health
Verify that all supporting databases and brokers are healthy:
```bash
docker compose -f deploy/docker/docker-compose.yaml ps
```

Default ports mapped locally:
| Service | Technology | Port | Credentials |
| :--- | :--- | :--- | :--- |
| **Identity & Catalog DB** | PostgreSQL | `5432` | `postgres` / `postgres` |
| **Distributed Cache** | Redis | `6379` | None (local dev) |
| **Event Bus** | RabbitMQ | `5672` (AMQP), `15672` (UI) | `guest` / `guest` |
| **Metrics** | Prometheus | `9090` | N/A |
| **Dashboards** | Grafana | `3000` | `admin` / `admin` |

---

## 4. Run via .NET Aspire AppHost (Recommended)

FreshCart is orchestrated using **.NET Aspire**. The `FreshCart.AppHost` automatically configures service discovery, connection strings, and resilience pipelines:

```bash
dotnet run --project src/FreshCart.AppHost/FreshCart.AppHost.csproj
```

Once started, the terminal will output the **Aspire Dashboard URL** (typically `https://localhost:17000`). Open it in your browser to inspect live metrics, structured logs, and distributed traces across all microservices.

---

## 5. Running Tests

Run the full suite of unit and component integration tests:
```bash
# Run all unit tests
dotnet test --filter "Category!=Integration"

# Run tests with detailed summary
dotnet test --logger "console;verbosity=normal"
```

---

## 6. Stopping the Environment

To tear down the Docker background containers:
```bash
docker compose -f deploy/docker/docker-compose.yaml down -v
```
