# Bicep infrastructure

Subscription-scope deployment that creates an entire FreshCart environment in Azure.

## Layout

```
infra/
├── main.bicep                  ← entrypoint, deploys env stack at sub scope
├── modules/
│   ├── network.bicep           ← VNet + subnets
│   ├── log-analytics.bicep     ← Log Analytics + Application Insights
│   ├── key-vault.bicep         ← Key Vault (RBAC, soft-delete, purge protection)
│   ├── acr.bicep               ← Container Registry (Premium + geo-replication for prod)
│   ├── aks.bicep               ← AKS (system + user node pools, Workload Identity, OIDC issuer, Cilium)
│   ├── sql.bicep               ← SQL Server + identitydb / orderingdb / inventorydb / paymentreaddb
│   ├── postgres.bicep          ← Postgres Flexible Server + catalogdb / basketdb
│   ├── mysql.bicep             ← MySQL Flexible Server + freshcart_reporting
│   ├── cosmos.bicep            ← Cosmos DB SQL API (serverless) + notificationdb / notifications
│   ├── redis.bicep             ← Azure Cache for Redis
│   ├── service-bus.bicep       ← Azure Service Bus + topics for events
│   ├── front-door.bicep        ← Azure Front Door + WAF
│   └── storage.bicep           ← Storage account + product-images / invoices / scheduled-reports
└── env/
    ├── dev.bicepparam
    ├── staging.bicepparam
    └── prod.bicepparam
```

## Deploy

```bash
# Validate (what-if)
az deployment sub what-if \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters @infra/env/dev.bicepparam

# Apply
az deployment sub create \
  --location eastus \
  --name freshcart-dev-$(date +%s) \
  --template-file infra/main.bicep \
  --parameters @infra/env/dev.bicepparam
```

## Teardown

```bash
az group delete --name freshcart-dev --yes --no-wait
```

## Secrets

Parameter files use `readEnvironmentVariable()` so the actual passwords come from the GitHub
Actions / Azure DevOps secret store at deploy time. Never check real values into the env
files.
