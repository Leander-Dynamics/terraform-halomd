project_name = "arbit"
env_name     = "qa"
location     = "eastus2"

# Secrets are injected at runtime via the pipeline / Key Vault.
# Ensure `sql_admin_login` and `sql_admin_password` are supplied securely
# (via variable groups, pipelines, or terraform.tfvars.d files).

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

# Subnet references
app_gateway_subnet_key              = "gateway"
app_gateway_subnet_id               = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-arbit-qa/providers/Microsoft.Network/virtualNetworks/vnet-arbit-qa/subnets/appgw"
bastion_subnet_key                  = "mgmt"
kv_private_endpoint_subnet_key      = "data"
storage_private_endpoint_subnet_key = "data"

# -------------------------
# Application Gateway
# -------------------------
app_gateway_fqdn_prefix = "agw-arbit-qa"
app_gateway_backend_fqdns = [
  "app-halomdweb-qa.azurewebsites.net",
  "app-arbit-arb-qa.azurewebsites.net"
]

# -------------------------
# DNS
# -------------------------
dns_zone_name = "az.halomd.com"

dns_a_records = {
  "api-qa" = {
    ttl     = 3600
    records = ["10.25.1.10"]
  }
}

dns_cname_records = {
  "web-qa" = {
    ttl    = 3600
    record = "app-arbit-qa.azurewebsites.net"
  }
}

# -------------------------
# Key Vault
# -------------------------
kv_public_network_access        = true
kv_network_acls                 = null
enable_kv_private_endpoint      = false
kv_private_dns_zone_ids         = []
kv_private_endpoint_resource_id = null

# -------------------------
# Storage Private Endpoint
# -------------------------
enable_storage_private_endpoint                = false
storage_private_dns_zone_ids                   = []
storage_private_endpoint_subresource_names     = ["blob"]
storage_account_private_connection_resource_id = null

# -------------------------
# App Service
# -------------------------
app_service_plan_sku    = "P1v3"
app_service_fqdn_prefix = "app-arbit-qa"_
