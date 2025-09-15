project_name    = "arbit"
env_name        = "dev"
location        = "eastus"
subscription_id = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id       = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

tags = {
  project = "arbit"
  env     = "dev"
  owner   = "platform"
}

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.10.0.0/16"]
subnets = {
  gateway = {
    address_prefixes = ["10.10.0.0/24"]
  }
  web = {
    address_prefixes = ["10.10.1.0/24"]
  }
}

app_gateway_subnet_id = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-arbit-dev/providers/Microsoft.Network/virtualNetworks/vnet-arbit-dev/subnets/appgw"

app_gateway_fqdn_prefix = "agw-arbit-dev"
app_gateway_backend_fqdns = []
app_gateway_backend_hostnames = [
  "web-arbit-dev.azurewebsites.net",
  "web-arbit-dev-arb.azurewebsites.net",
]

# -------------------------
# DNS
# -------------------------
dns_zone_name = "az.halomd.com"

dns_a_records = {
  "api-dev" = {
    ttl     = 3600
    records = ["10.10.1.10"]
  }
}

dns_cname_records = {
  "web-dev" = {
    ttl    = 3600
    record = "app-arbit-dev.azurewebsites.net"
  }
}

# -------------------------
# App Service
# -------------------------
app_service_plan_sku    = "B1"
app_service_fqdn_prefix = "app-arbit-dev"
app_service_app_settings = {
  "WEBSITE_RUN_FROM_PACKAGE" = "0"
}
app_service_connection_strings = {
  PrimaryDatabase = {
    type  = "SQLAzure"
    value = "Server=tcp:sql-arbit-dev.database.windows.net,1433;Initial Catalog=halomd;User ID=sqladmin;Password=P@ssw0rd1234!;Encrypt=True;"
  }
}

# -------------------------
# Feature flags
# -------------------------
enable_aks      = false
enable_acr      = false
enable_storage  = false
enable_sql      = true
kv_public_network_access = true

# -------------------------
# ACR / AKS / Function
# -------------------------
acr_sku       = "Basic"
aks_node_count = 1
aks_vm_size   = "Standard_DS2_v2"
web_plan_sku  = "B1"
func_plan_sku = "Y1"

web_dotnet_version      = "8.0"
function_external_runtime = "dotnet"
function_cron_runtime     = "python"

# -------------------------
# Arbitration app
# -------------------------
arbitration_plan_sku        = "B1"
arbitration_runtime_stack   = "dotnet"
arbitration_runtime_version = "8.0"
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

# -------------------------
# SQL Serverless
# -------------------------
sql_database_name        = "halomd"
sql_db_name              = "halomd"
sql_sku_name             = "GP_S_Gen5_2"
sql_auto_pause_minutes   = 60
sql_auto_pause_delay     = 60
sql_max_size_gb          = 75
sql_min_capacity         = 0.5
sql_max_capacity         = 4
sql_public_network_access = true
sql_admin_login          = "sqladmin"
sql_admin_password       = "P@ssw0rd1234!"

sql_firewall_rules = [
  {
    name             = "allow-all"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
