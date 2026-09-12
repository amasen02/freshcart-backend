# Local Development Quickstart Guide

This guide walks you through bootstrapping the **FreshCart** microservices backend with .NET Aspire. A Docker Compose alternative is included for developers who want to run the services manually.

---

## 1. Prerequisites

Ensure you have the following installed on your workstation:
- **.NET SDK 10.0.100** (the version selected by `global.json`) &mdash; [Download .NET](https://dotnet.microsoft.com/download)
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

## 3. Run the Complete Stack with Aspire (Recommended)

The AppHost creates the backing stores and broker, starts all backend services, and supplies their service-discovery and connection-string configuration. Do not start the Compose stack as well, because that duplicates the same infrastructure and can cause port conflicts.

```bash
dotnet run --project src/AspireAppHost/FreshCart.AppHost/FreshCart.AppHost.csproj --launch-profile http
```

The terminal prints the Aspire Dashboard URL. Open it to inspect service health, logs, metrics, and traces.

## 4. Compose Alternative for Manual Service Runs

Use Compose only when you intend to run and configure the .NET services yourself:

```bash
docker compose -f deploy/docker/docker-compose.yaml up -d
docker compose -f deploy/docker/docker-compose.yaml ps
```

Stop that alternative with:

```bash
docker compose -f deploy/docker/docker-compose.yaml down -v
```

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

## 6. Stopping the Aspire Environment

Stop the AppHost process with `Ctrl+C`. Aspire owns the containers it started; the normal developer configuration keeps their data volumes for the next run.
