project_name = "arbit"
location = "eastus"

env_name = "dev"
subscription_id = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

app_insights_name                   = "arbit-dev-appi"
app_insights_resource_group_name    = "arbit-dev-rg"
tags = {
  project = "arbit"
  env = "dev"
  owner = "platform"
}

dns_zone_name = "az.halomd.com"

dns_a_records = {
  "api-dev" = {
    ttl     = 3600
    records = ["10.0.0.10"]
  }
}

dns_cname_records = {
  "web-dev" = {
    ttl   = 3600
    record = "web-arbit-dev.azurewebsites.net"
  }
  "func-ext-dev" = {
    ttl   = 3600
    record = "func-ext-arbit-dev.azurewebsites.net"
  }
  "func-cron-dev" = {
    ttl   = 3600
    record = "func-cron-arbit-dev.azurewebsites.net"
  }
}

enable_aks = false
enable_acr = false
enable_storage = false
enable_sql = true
kv_public_network_access = true

acr_sku = "Basic"
aks_node_count = 1
aks_vm_size = "Standard_DS2_v2"
web_plan_sku = "B1"
func_plan_sku = "Y1"

web_dotnet_version = "8.0"
function_external_runtime = "dotnet"
function_cron_runtime = "python"

arbitration_plan_sku         = "B1"
arbitration_runtime_stack    = "dotnet"
arbitration_runtime_version  = "8.0"
arbitration_connection_strings = {
  ConnStr = {
    type  = "SQLAzure"
    value = "Server=tcp:dev-arbit-sql.database.windows.net,1433;Initial Catalog=dev-arbit-db;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;"
  }
  IDRConnStr = {
    type  = "SQLAzure"
    value = "Server=tcp:dev-idr-sql.database.windows.net,1433;Initial Catalog=dev-idr-db;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;"
  }
}
arbitration_app_settings = {
  "Storage__Connection" = "DefaultEndpointsProtocol=https;AccountName=devarbitstorage;AccountKey=FakeKeyForDev==;EndpointSuffix=core.windows.net"
  "Storage__Container"  = "arbitration-calculator"
}

sql_db_name = "halomd"
sql_sku_name = "GP_S_Gen5_2"
sql_auto_pause_minutes = 60
sql_max_size_gb = 75
sql_public_network_access = true
sql_firewall_rules = [
  {
    name             = "allow-any-sql"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
sql_admin_login = "sqladmin"
sql_admin_password = "P@ssw0rd1234!"

sql_firewall_rules = [
  {
    name             = "allow-any-sql"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
