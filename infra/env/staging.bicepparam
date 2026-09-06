using '../main.bicep'

param environmentName = 'staging'
param location = 'eastus'

// Secrets are supplied as environment variables by the deployment pipeline (see
// .github/workflows/infrastructure.yml). The single-argument readEnvironmentVariable has no default, so a
// missing variable fails the deployment instead of quietly provisioning a placeholder administrator.
param sqlAdministratorLogin = readEnvironmentVariable('FRESHCART_SQL_ADMIN_LOGIN', 'freshcartsa')
param sqlAdministratorPassword = readEnvironmentVariable('FRESHCART_SQL_ADMIN_PASSWORD')
param postgresAdministratorLogin = readEnvironmentVariable('FRESHCART_PG_ADMIN_LOGIN', 'freshcartpg')
param postgresAdministratorPassword = readEnvironmentVariable('FRESHCART_PG_ADMIN_PASSWORD')
param mysqlAdministratorLogin = readEnvironmentVariable('FRESHCART_MYSQL_ADMIN_LOGIN', 'freshcartmy')
param mysqlAdministratorPassword = readEnvironmentVariable('FRESHCART_MYSQL_ADMIN_PASSWORD')
param keyVaultAdministratorObjectId = readEnvironmentVariable('FRESHCART_KV_ADMIN_OBJECT_ID')
