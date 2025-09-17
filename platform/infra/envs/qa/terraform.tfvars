project_name = "arbit"
env_name     = "qa"
location     = "eastus"

# Secrets are injected at runtime via the pipeline / Key Vault.

tags = {
  project = "arbit"
  env     = "qa"
  owner   = "platform"
}

# -------------------------
# Monitoring
# -------------------------
log_analytics_workspace_name = "log-arbit-qa"
application_insights_name    = "appi-arbit-qa"

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.15.0.0/16"]
subnets = {
  gateway = {
    address_prefixes = ["10.15.0.0/24"]
  }
  web = {
    address_prefixes = ["10.15.1.0/24"]
  }
}

# Reference subnets by key (no hard-coded IDs)
app_gateway_subnet_key = "gateway"

app_gateway_fqdn_prefix   = "agw-arbit-qa"
app_gateway_backend_fqdns = []

# -------------------------
# DNS
# -------------------------
dns_zone_name = "az.halomd.com"

dns_a_records = {
  "api-qa" = {
    ttl     = 3600
    records = ["10.15.1.10"]
  }
}

dns_cname_records = {
  "web-qa" = {
    ttl    = 3600
    record = "app-arbit-qa.azurewebsites.net"
  }
}

# -------------------------
# App Service
# -------------------------
app_service_plan_sku    = "S1"
plan_sku                = "S1"
app_service_fqdn_prefix = "app-arbit-qa"
app_service_app_settings = {
  "WEBSITE_RUN_FROM_PACKAGE" = "0"
}
app_service_connection_strings = {
  PrimaryDatabase = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/app-service-primary-database-connection)"
  }
}

# -------------------------
# Feature flags
# -------------------------
enable_acr               = false
enable_sql               = true
kv_public_network_access = true

# -------------------------
# Arbitration app
# -------------------------
arbitration_plan_sku        = "S1"
arbitration_runtime_stack   = "dotnet"
arbitration_runtime_version = "8.0"

arbitration_connection_strings = [
  {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/arbitration-primary-connection)"
  }
]

arbitration_app_settings = {
  "Storage__Connection" = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/arbitration-storage-connection)"
  "Storage__Container"  = "arbitration-calculator"
}

# -------------------------
# SQL Serverless
# -------------------------
sql_database_name         = "halomd"
sql_sku_name              = "GP_S_Gen5_2"
sql_auto_pause_delay      = 60
sql_max_size_gb           = 96
sql_min_capacity          = 0.75
sql_max_capacity          = 5
sql_public_network_access = true
sql_admin_login           = ""
sql_admin_password        = ""

sql_firewall_rules = [
  {
    name             = "allow-all"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
