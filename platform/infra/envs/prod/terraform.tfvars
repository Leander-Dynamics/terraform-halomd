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
enable_sql      = true
kv_public_network_access = true

acr_sku        = "Premium"
aks_node_count = 3
aks_vm_size    = "Standard_DS3_v2"
web_plan_sku   = "P1v3"
func_plan_sku  = "Y1"

web_dotnet_version        = "8.0"
function_external_runtime = "dotnet"
function_cron_runtime     = "python"

arbitration_plan_sku         = "P1v3"
arbitration_runtime_stack    = "dotnet"
arbitration_runtime_version  = "8.0"
arbitration_connection_strings = {
  ConnStr = {
    type  = "SQLAzure"
    value = "Server=tcp:prod-arbit-sql.database.windows.net,1433;Initial Catalog=prod-arbit-db;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;"
  }
  IDRConnStr = {
    type  = "SQLAzure"
    value = "Server=tcp:prod-idr-sql.database.windows.net,1433;Initial Catalog=prod-idr-db;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;"
  }
}
arbitration_app_settings = {
  "Storage__Connection" = "DefaultEndpointsProtocol=https;AccountName=prodarbitstorage;AccountKey=FakeKeyForProd==;EndpointSuffix=core.windows.net"
  "Storage__Container"  = "arbitration-calculator"
}

sql_db_name               = "halomd"
sql_sku_name              = "GP_S_Gen5_2"
sql_auto_pause_minutes    = 60
sql_max_size_gb           = 32
sql_public_network_access = true
sql_firewall_rules = [
  {
    name             = "allow-any-sql"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
# sql_admin_login    = ""
# sql_admin_password = ""

sql_firewall_rules = [
  {
    name             = "allow-any-sql"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
