project_name = "arbit"
env_name     = "stage"
location     = "eastus"

tags = {
  project = "arbit"
  env     = "stage"
  owner   = "platform"
}

# -------------------------
# Feature flags
# -------------------------
enable_sql = true

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
app_gateway_subnet_key = "gateway"

# For module using direct subnet id
app_gateway_subnet_id = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-arbit-stage/providers/Microsoft.Network/virtualNetworks/vnet-arbit-stage/subnets/appgw"

app_gateway_fqdn_prefix = "agw-arbit-stage"
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
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-stage.vault.azure.net/secrets/app-service-primary-database-connection)"
  }
}

# -------------------------
# Arbitration App
# -------------------------
arbitration_plan_sku        = "P1v3"
arbitration_runtime_stack   = "dotnet"
arbitration_runtime_version = "8.0"
arbitration_storage_container_name = "arbitration-calculator"

arbitration_app_settings = {
  "Storage__Connection" = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-stage.vault.azure.net/secrets/arbitration-storage-connection)"
  "Storage__Container"  = "arbitration-calculator"
}

arbitration_connection_strings = {
  DefaultConnection = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-stage.vault.azure.net/secrets/arbitration-primary-connection)"
  }
}

# -------------------------
# SQL Database
# -------------------------
sql_database_name         = "halomd"
sql_sku_name              = "GP_S_Gen5_2"
sql_max_size_gb           = 64
sql_auto_pause_delay      = 60
sql_min_capacity          = 1
sql_max_capacity          = 6
sql_public_network_access = true

# ✅ Use Key Vault for secure secret injection
sql_admin_login                = "sqladminstage"
sql_admin_password             = null
sql_admin_password_secret_name = "sql-admin-password-stage"

sql_firewall_rules = [
  {
    name             = "allow-all"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
