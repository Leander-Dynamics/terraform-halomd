project_name = "arbit"
location     = "eastus"
env_name = "prod"
subscription_id = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id       = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

tags = {
  project = "arbit"
  env     = "prod"
  owner   = "platform"
}

enable_aks      = false
enable_acr      = false
enable_storage  = false
enable_sql      = false
kv_public_network_access = true

acr_sku        = "Premium"
aks_node_count = 3
aks_vm_size    = "Standard_DS3_v2"
web_plan_sku   = "P1v3"
func_plan_sku  = "Y1"

web_dotnet_version        = "8.0"
function_external_runtime = "dotnet"
function_cron_runtime     = "python"

sql_db_name               = "halomd"
sql_sku_name              = "GP_S_Gen5_2"
sql_auto_pause_minutes    = 60
sql_max_size_gb           = 32
sql_public_network_access = true
# sql_admin_login    = ""
# sql_admin_password = ""
