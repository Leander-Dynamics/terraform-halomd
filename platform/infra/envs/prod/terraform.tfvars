project_name = "arbit"
env_name     = "prod"
location     = "eastus2"

# Secrets are injected at runtime via the pipeline / Key Vault.
# Ensure `sql_admin_login` and `sql_admin_password` are supplied securely (for example,
# via an untracked terraform.tfvars file, a variable group, or Key Vault) before
# running `terraform plan` or `terraform apply`.

tags = {
  project = "arbit"
  env     = "prod"
  owner   = "platform"
}

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.40.0.0/16"]
subnets = {
  gateway = { address_prefixes = ["10.40.0.0/24"] }
  web     = { address_prefixes = ["10.40.1.0/24"] }
  data    = { address_prefixes = ["10.40.2.0/24"] }
  mgmt    = { address_prefixes = ["10.40.3.0/24"] }
}

# For module using subnet keys
app_gateway_subnet_key  = "gateway"

# For module using direct subnet id
app_gateway_subnet_id = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-arbit-prod/providers/Microsoft.Network/virtualNetworks/vnet-arbit-prod/subnets/appgw"

app_gateway_fqdn_prefix   = "agw-arbit-prod"
app_gateway_backend_fqdns = [
  "app-halomdweb-prod.azurewebsites.net",
  "app-arbit-arb-prod.azurewebsites.net",
]

# -------------------------
# DNS
# -------------------------
dns_zone_name = "az.halomd.com"

dns_a_records = {
  "api-prod" = {
    ttl     = 3600
    records = ["10.40.1.10"]
  }
}

dns_cname_records = {
  "web-prod" = {
    ttl    = 3600
    record = "app-arbit-prod.azurewebsites.net"
  }
}

# -------------------------
# App Service
# -------------------------
app_service_plan_sku    = "P2v3"
app_service_fqdn_prefix = "app-arbit-prod"
app_service_app_settings = {
  "WEBSITE_RUN_FROM_PACKAGE" = "0"
}
app_service_connection_strings = {
  PrimaryDatabase = {
    type  = "SQLAzure"
    value = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-prod.vault.azure.net/secrets/app-service-primary-database-connection)"
  }
}

# -------------------------
# Arbitration App
# -------------------------
arbitration_app_settings = {
  "Storage__Connection" = "@Microsoft.KeyVault(SecretUri=https://kv-arbit-prod.vault.azure.net/secrets/arbitration-storage-connection)"
  "Storage__Container"  = "arbitration-calculator"
}

# -------------------------
# SQL Database
# -------------------------
# Support both sql_database_name and sql_db_name for different modules
sql_database_name        = "halomd"

# Extended config
sql_sku_name             = "GP_S_Gen5_4"
sql_max_size_gb          = 128
sql_auto_pause_delay     = 60
sql_min_capacity         = 2
sql_max_capacity         = 8
sql_public_network_access = true

# Firewall rules
sql_firewall_rules = [
  {
    name             = "allow-azure-services"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "0.0.0.0"
  }
]
