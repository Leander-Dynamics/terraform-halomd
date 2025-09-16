project_name = "arbit"
env_name     = "dev"
location     = "eastus"

# Secrets are injected at runtime via the pipeline / Key Vault.
# Ensure `sql_admin_login` and `sql_admin_password` are supplied securely (for example,
# via an untracked terraform.tfvars file, a variable group, or Key Vault) before
# running `terraform plan` or `terraform apply`.

tags = {
  project = "arbit"
  env     = "dev"
  owner   = "platform"
}

# -------------------------
# Monitoring
# -------------------------
log_analytics_workspace_name    = "log-arbit-dev"
application_insights_name       = "appi-arbit-dev"
log_analytics_retention_in_days = 30
log_analytics_daily_quota_gb    = -1

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.20.0.0/16"]
subnets = {
  gateway = { address_prefixes = ["10.20.0.0/24"] }
  web     = { address_prefixes = ["10.20.1.0/24"] }
  data    = { address_prefixes = ["10.20.2.0/24"] }
  mgmt    = { address_prefixes = ["10.20.3.0/24"] }
}

# Subnet references for optional modules
bastion_subnet_key                  = "mgmt"
kv_private_endpoint_subnet_key      = "data"
storage_private_endpoint_subnet_key = "data"

kv_public_network_access = true

# -------------------------
# App Service
# -------------------------
app_service_plan_sku                     = "B1"
app_service_app_insights_connection_string = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-dev.vault.azure.net/secrets/app-service-appinsights-connection-string)"
app_service_app_settings                 = {}
app_service_connection_strings = {
  PrimaryDatabase = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-dev.vault.azure.net/secrets/app-service-primary-database-connection)"
  }
}

# -------------------------
# Arbitration App
# -------------------------
enable_arbitration_app_service          = true
arbitration_app_settings = {
  "Storage__Connection" = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-dev.vault.azure.net/secrets/arbitration-storage-connection)"
  "Storage__Container"  = "arbitration-calculator"
}
# Required keys:
#   - ConnStr: primary arbitration database
#   - IDRConnStr: IDR arbitration database
arbitration_connection_strings = {
  ConnStr = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-dev.vault.azure.net/secrets/arbitration-primary-connection)"
  }
  IDRConnStr = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-dev.vault.azure.net/secrets/arbitration-idr-connection)"
  }
}
arbitration_app_insights_connection_string = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-dev.vault.azure.net/secrets/arbitration-appinsights-connection-string)"
