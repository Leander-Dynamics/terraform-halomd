project_name = "arbit"
location     = "eastus"

env_name            = "dev"
container_name      = "arbit"
subscription_id     = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id           = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"
resource_group_name = "dev-eus2-ops-rg-1"
storage_account_name = "deveus2terraform"
key                 = "arbit/dev.tfstate"
use_azuread_auth    = true

deploy_rg = "arbit-dev-rg"

app_insights_name             = "arbit-dev-appi"
app_insights_rg               = "arbit-dev-rg"
app_insights_connection_string = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example.com/"

tags = {
  project = "arbit"
  env     = "dev"
  owner   = "platform"
}

enable_aks      = false
enable_acr      = false
enable_storage  = false
enable_sql      = false
kv_public_network_access = true

acr_sku        = "Basic"
aks_node_count = 1
aks_vm_size    = "Standard_DS2_v2"
plan_sku       = "B1"

web_dotnet_version        = "8.0"
function_external_runtime = "dotnet"
function_cron_runtime     = "python"

sql_db_name               = "halomd"
sql_sku_name              = "GP_S_Gen5_2"
sql_auto_pause_minutes    = 60
sql_max_size_gb           = 32
sql_public_network_access = true
sql_admin_login           = "sqladmin"
sql_admin_password        = "P@ssw0rd1234!"
