project_name = "arbit"
env_name     = "stage"
location     = "eastus"

tags = {
  project = "arbit"
  env     = "stage"
  owner   = "platform"
}

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.20.0.0/16"]
subnets = {
  gateway = {
    address_prefixes = ["10.20.0.0/24"]
  }
  web = {
    address_prefixes = ["10.20.1.0/24"]
  }
}

# For module using subnet keys
app_gateway_subnet_key  = "gateway"

# For module using direct subnet id
app_gateway_subnet_id = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-arbit-stage/providers/Microsoft.Network/virtualNetworks/vnet-arbit-stage/subnets/appgw"

app_gateway_fqdn_prefix   = "agw-arbit-stage"
app_gateway_backend_fqdns = [
  "app-halomdweb-stage.azurewebsites.net",
  "app-arbit-arb-stage.azurewebsites.net",
]

# -------------------------
# DNS
# -------------------------
dns_zone_name = "az.halomd.com"

dns_a_records = {
  "api-stage" = {
    ttl     = 3600
    records = ["10.20.1.10"]
  }
}

dns_cname_records = {
  "web-stage" = {
    ttl    = 3600
    record = "app-arbit-stage.azurewebsites.net"
  }
}

# -------------------------
# App Service
# -------------------------
app_service_plan_sku    = "P1v3"
app_service_fqdn_prefix = "app-arbit-stage"
app_service_app_settings = {
  "WEBSITE_RUN_FROM_PACKAGE" = "0"
}
app_service_connection_strings = {
  PrimaryDatabase = {
    type  = "SQLAzure"
    value = "Server=tcp:sql-arbit-stage.database.windows.net,1433;Initial Catalog=halomd;User ID=sqladminstage;Password=P@ssw0rd123!Stage;Encrypt=True;"
  }
}

# -------------------------
# Arbitration App
# -------------------------
arbitration_storage_container_name = "arbitration-calculator"

arbitration_app_settings = {
  "Storage__Connection" = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-stage.vault.azure.net/secrets/arbitration-storage-connection)"
  "Storage__Container"  = "arbitration-calculator"
}

# -------------------------
# SQL Database
# -------------------------
# Support both sql_database_name and sql_db_name for different modules
sql_database_name        = "halomd"

# Extended config
sql_sku_name             = "GP_S_Gen5_2"
sql_max_size_gb          = 64
sql_auto_pause_delay     = 60
sql_min_capacity         = 1
sql_max_capacity         = 6
sql_public_network_access = true

sql_admin_login    = "sqladminstage"
sql_admin_password = "P@ssw0rd123!Stage"

# Firewall rules
sql_firewall_rules = [
  {
    name             = "allow-all"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
