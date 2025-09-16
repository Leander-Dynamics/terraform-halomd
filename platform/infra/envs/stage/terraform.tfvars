project_name = "arbit"
env_name     = "stage"
location     = "eastus"

# Secrets are injected at runtime via the pipeline / Key Vault.
# Ensure `sql_admin_login` and `sql_admin_password` are supplied securely (for example,
# via an untracked terraform.tfvars file, a variable group, or Key Vault) before
# running `terraform plan` or `terraform apply`.

tags = {
  project = "arbit"
  env     = "stage"
  owner   = "platform"
}

# -------------------------
# Monitoring
# -------------------------
log_analytics_workspace_name    = "log-arbit-stage"
application_insights_name       = "appi-arbit-stage"
log_analytics_retention_in_days = 60
log_analytics_daily_quota_gb    = -1

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.30.0.0/16"]
subnets = {
  gateway = { address_prefixes = ["10.30.0.0/24"] }
  web     = { address_prefixes = ["10.30.1.0/24"] }
  data    = { address_prefixes = ["10.30.2.0/24"] }
  mgmt    = { address_prefixes = ["10.30.3.0/24"] }
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
    records = ["10.30.1.10"]
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

# Firewall rules
sql_firewall_rules = [
  {
    name             = "allow-azure-services"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "0.0.0.0"
  }
]
