project_name = "arbit"
env_name     = "qa"
location     = "eastus2"

# Secrets are injected at runtime via the pipeline / Key Vault.
# Ensure `sql_admin_login` and `sql_admin_password` are supplied securely (for example,
# via an untracked terraform.tfvars file, a variable group, or Key Vault) before
# running `terraform plan` or `terraform apply`.

tags = {
  project = "arbit"
  env     = "qa"
  owner   = "platform"
}

# -------------------------
# Monitoring
# -------------------------
log_analytics_workspace_name    = "log-arbit-qa"
application_insights_name       = "appi-arbit-qa"
log_analytics_retention_in_days = 60
log_analytics_daily_quota_gb    = -1

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.56.32.0/19"]
subnets = {
  gateway = { address_prefixes = ["10.56.32.0/23"] }
  web     = { address_prefixes = ["10.56.34.0/24"] }
  data    = { address_prefixes = ["10.56.35.0/24"] }
  mgmt    = { address_prefixes = ["10.56.36.0/23"] }
}

# Subnet references for optional modules
bastion_subnet_key                  = "mgmt"
kv_private_endpoint_subnet_key      = "data"
storage_private_endpoint_subnet_key = "data"

# Key Vault configuration
kv_public_network_access        = true
kv_network_acls                 = null
enable_kv_private_endpoint      = false
kv_private_dns_zone_ids         = []
kv_private_endpoint_resource_id = null

# Storage private endpoint configuration
enable_storage_private_endpoint                = false
storage_private_dns_zone_ids                   = []
storage_private_endpoint_subresource_names     = ["blob"]
storage_account_private_connection_resource_id = null

# -------------------------
# App Service
# -------------------------
app_service_plan_sku    = "P1v3"
app_service_fqdn_prefix = "app-arbit-qa"
app_service_app_insights_connection_string = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/app-servi
ce-appinsights-connection-string)"
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
# Arbitration App
# -------------------------
enable_arbitration_app_service = true
arbitration_app_settings = {
  "Storage__Connection" = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/arbitration-storage-connecti
on)"
  "Storage__Container"  = "arbitration-calculator"
}
# Required keys:
#   - ConnStr: primary arbitration database
#   - IDRConnStr: IDR arbitration database
arbitration_connection_strings = {
  ConnStr = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/arbitration-primary-connection)"
  }
  IDRConnStr = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/arbitration-idr-connection)"
  }
}
arbitration_app_insights_connection_string = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-qa.vault.azure.net/secrets/arbitratio
n-appinsights-connection-string)"

# -------------------------
# SQL Database
# -------------------------
# Support both sql_database_name and sql_db_name for different modules
sql_database_name = "halomd"

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
