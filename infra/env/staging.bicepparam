using '../main.bicep'

param environmentName = 'staging'
param location = 'eastus'

param sqlAdministratorLogin = readEnvironmentVariable('FRESHCART_SQL_ADMIN_LOGIN', 'freshcartsa')
param sqlAdministratorPassword = readEnvironmentVariable('FRESHCART_SQL_ADMIN_PASSWORD', 'replace-in-pipeline')
param postgresAdministratorLogin = readEnvironmentVariable('FRESHCART_PG_ADMIN_LOGIN', 'freshcartpg')
param postgresAdministratorPassword = readEnvironmentVariable('FRESHCART_PG_ADMIN_PASSWORD', 'replace-in-pipeline')
param mysqlAdministratorLogin = readEnvironmentVariable('FRESHCART_MYSQL_ADMIN_LOGIN', 'freshcartmy')
param mysqlAdministratorPassword = readEnvironmentVariable('FRESHCART_MYSQL_ADMIN_PASSWORD', 'replace-in-pipeline')
param keyVaultAdministratorObjectId = readEnvironmentVariable('FRESHCART_KV_ADMIN_OBJECT_ID', '00000000-0000-0000-0000-000000000000')
