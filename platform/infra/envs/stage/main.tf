# Local Variables
locals {
  rg_name          = "rg-${var.project_name}-${var.env_name}"
  kv_name          = "kv-${var.project_name}-${var.env_name}"
  log_name         = "log-${var.project_name}-${var.env_name}"
  appi_name        = "appi-${var.project_name}-${var.env_name}"
  aad_app_display  = "aad-${var.project_name}-${var.env_name}"

  acr_name         = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name         = "aks-${var.project_name}-${var.env_name}-${var.location}"

  web_plan         = "asp-halomdweb-${var.env_name}-${var.location}"
  web_name         = "app-halomdweb-${var.env_name}"
  app_gateway_name = "agw-${var.project_name}-${var.env_name}"
  arbitration_plan = "asp-${var.project_name}-arb-${var.env_name}-${var.location}"
  arbitration_name = "app-${var.project_name}-arb-${var.env_name}"

  arbitration_plan_sku_effective        = var.arbitration_plan_sku != "" ? trimspace(var.arbitration_plan_sku) : "B1"
  arbitration_runtime_stack_effective   = var.arbitration_runtime_stack != "" ? trimspace(var.arbitration_runtime_stack) : "dotnet"
  arbitration_runtime_version_effective = var.arbitration_runtime_version != "" ? trimspace(var.arbitration_runtime_version) : "8.0"

  storage_data_name = lower(replace("st${var.project_name}${var.env_name}data", "-", ""))

  func_external_plan = "asp-external-${var.env_name}-${var.location}"
  func_external_name = "func-external-${var.env_name}"
  func_cron_plan     = "asp-cron-${var.env_name}-${var.location}"
  func_cron_name     = "func-cron-${var.env_name}"

  sql_server_name   = "sql-${var.project_name}-${var.env_name}"
  sql_database_name = var.sql_database_name != "" ? var.sql_database_name : "${var.project_name}-${var.env_name}"
  sql_admin_login_effective    = trimspace(coalesce(var.sql_admin_login, ""))
  sql_admin_password_effective = coalesce(var.sql_admin_password, "")
}

# Core Modules
module "resource_group" {
  source   = "../../Azure/modules/resource-group"
  name     = local.rg_name
  location = var.location
  tags     = var.tags
}

module "network" {
  source              = "../../Azure/modules/network"
  name                = "vnet-${var.project_name}-${var.env_name}"
  resource_group_name = module.resource_group.name
  location            = var.location
  address_space       = var.vnet_address_space
  dns_servers         = var.vnet_dns_servers
  subnets             = var.subnets
  tags                = var.tags
}

# Storage
module "arbitration_storage_account" {
  source              = "../../Azure/modules/storage-account"
  name                = local.storage_data_name
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = var.tags
}

module "arbitration_storage_container" {
  source               = "../../Azure/modules/storage-container"
  name                 = var.arbitration_storage_container_name
  storage_account_name = module.arbitration_storage_account.name
}

# App Service (Web)
module "app_service" {
  source              = "../../Azure/modules/app-service"
  plan_name           = local.web_plan
  plan_sku            = var.app_service_plan_sku
  plan_os_type        = var.app_service_plan_os_type
  app_name            = var.app_service_fqdn_prefix
  resource_group_name = module.resource_group.name
  location            = var.location
  https_only          = var.app_service_https_only
  always_on           = var.app_service_always_on
  app_settings        = var.app_service_app_settings
  connection_strings  = var.app_service_connection_strings
  tags                = var.tags
}

# Application Gateway
module "app_gateway" {
  source              = "../../Azure/modules/app-gateway"
  name                = local.app_gateway_name
  resource_group_name = module.resource_group.name
  location            = var.location
  subnet_id           = module.network.subnet_ids[var.app_gateway_subnet_key]
  fqdn_prefix         = var.app_gateway_fqdn_prefix
  backend_fqdns       = distinct(concat(var.app_gateway_backend_fqdns, [module.app_service.default_hostname]))
  backend_port        = var.app_gateway_backend_port
  backend_protocol    = var.app_gateway_backend_protocol
  frontend_port       = var.app_gateway_frontend_port
  listener_protocol   = var.app_gateway_listener_protocol
  sku_name            = var.app_gateway_sku_name
  sku_tier            = var.app_gateway_sku_tier
  sku_capacity        = var.app_gateway_capacity
  enable_http2        = var.app_gateway_enable_http2
  backend_request_timeout          = var.app_gateway_backend_request_timeout
  pick_host_name_from_backend_address = var.app_gateway_pick_host_name
  tags                = var.tags
}

# SQL
module "sql" {
  count                         = var.enable_sql && local.sql_admin_login_effective != "" && local.sql_admin_password_effective != "" ? 1 : 0
  source                        = "../../Azure/modules/sql-serverless"
  server_name                   = local.sql_server_name
  database_name                 = local.sql_database_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  administrator_login           = var.sql_admin_login
  administrator_password        = var.sql_admin_password
  public_network_access_enabled = var.sql_public_network_access
  minimum_tls_version           = var.sql_minimum_tls_version
  sku_name                      = var.sql_sku_name
  auto_pause_delay_in_minutes   = var.sql_auto_pause_delay
  max_size_gb                   = var.sql_max_size_gb
  min_capacity                  = var.sql_min_capacity
  max_capacity                  = var.sql_max_capacity
  read_scale                    = var.sql_read_scale
  zone_redundant                = var.sql_zone_redundant
  collation                     = var.sql_collation
  firewall_rules                = var.sql_firewall_rules
  tags                          = var.tags
}

# DNS
module "dns_zone" {
  source              = "../../Azure/modules/dns-zone"
  zone_name           = var.dns_zone_name
  resource_group_name = module.resource_group.name
  tags                = var.tags
  a_records           = var.dns_a_records
  cname_records       = var.dns_cname_records
}

# App Insights
module "app_insights" {
  source                       = "../../Azure/modules/app-insights"
  resource_group_name          = coalesce(var.app_insights_resource_group_name, module.resource_group.name)
  location                     = var.location
  log_analytics_workspace_name = local.log_name
  application_insights_name    = local.appi_name
  tags                         = var.tags
}

# App Service: Arbitration
module "app_service_arbitration" {
  source                        = "../../Azure/modules/app-service-arbitration"
  name                          = local.arbitration_name
  plan_name                     = local.arbitration_plan
  plan_sku                      = local.arbitration_plan_sku_effective
  resource_group_name           = module.resource_group.name
  location                      = var.location
  runtime_stack                 = local.arbitration_runtime_stack_effective
  runtime_version               = local.arbitration_runtime_version_effective
  app_insights_connection_string = module.app_insights.application_insights_connection_string
  log_analytics_workspace_id     = module.app_insights.log_analytics_workspace_id
  connection_strings            = var.arbitration_connection_strings
  app_settings                  = var.arbitration_app_settings
  tags                          = var.tags
}

# AAD Application
module "aad_app" {
  source       = "../../Azure/modules/aad-app"
  display_name = local.aad_app_display
}

# Key Vault Setup
locals {
  kv_secret_input_values = merge({
    "sql-admin-login"                         = var.sql_admin_login,
    "sql-admin-password"                      = var.sql_admin_password,
    "app-service-primary-database-connection" = var.app_service_primary_database_connection_string,
    "arbitration-primary-connection"          = var.arbitration_primary_connection_string,
    "arbitration-idr-connection"              = var.arbitration_idr_connection_string,
    "arbitration-storage-connection"          = module.arbitration_storage_account.primary_connection_string,
    "aad-application-client-id"               = module.aad_app.client_id,
    "aad-application-object-id"               = module.aad_app.object_id,
  }, var.kv_additional_secrets)

  kv_secrets = {
    for name, value in local.kv_secret_input_values :
    name => {
      value = value
    }
    if try(trim(value), "") != ""
  }

  kv_rbac_assignments = merge(
    {
      arbitration_app = {
        principal_id         = module.app_service_arbitration.principal_id
        role_definition_name = "Key Vault Secrets User"
      }
    },
    var.kv_cicd_principal_id != "" ? {
      cicd = {
        principal_id         = var.kv_cicd_principal_id
        role_definition_name = "Key Vault Secrets User"
      }
    } : {}
  )
}

module "kv" {
  source                        = "../../Azure/modules/key-vault"
  name                          = local.kv_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  public_network_access_enabled = var.kv_public_network_access
  enable_rbac_authorization     = true
  secrets                       = local.kv_secrets
  rbac_assignments              = local.kv_rbac_assignments
  tags                          = var.tags
}

# -------------------------
# Outputs
# -------------------------
output "resource_group_name" {
  value       = module.resource_group.name
  description = "Resource group provisioned for the environment."
}

output "virtual_network_id" {
  value       = module.network.virtual_network_id
  description = "ID of the deployed virtual network."
}

output "app_service_default_hostname" {
  value       = module.app_service.default_hostname
  description = "Default hostname assigned to the App Service."
}

output "app_gateway_id" {
  value       = module.app_gateway.id
  description = "ID of the Application Gateway."
}

output "app_gateway_public_ip_address" {
  value       = module.app_gateway.public_ip_address
  description = "Allocated public IP address of the Application Gateway."
}

output "app_gateway_public_fqdn" {
  value       = module.app_gateway.public_ip_fqdn
  description = "Public FQDN assigned to the Application Gateway."
}

output "sql_server_fqdn" {
  value       = var.enable_sql ? module.sql[0].server_fqdn : null
  description = "Fully qualified domain name of the SQL Server."
}

output "sql_database_id" {
  value       = var.enable_sql ? module.sql[0].database_id : null
  description = "Database resource ID."
}

output "sql_server_name" {
  value       = var.enable_sql ? module.sql[0].server_name : null
  description = "SQL Server name."
}

output "app_insights_connection_string" {
  value       = module.app_insights.application_insights_connection_string
  description = "Application Insights connection string."
}

output "app_insights_instrumentation_key" {
  value       = module.app_insights.application_insights_instrumentation_key
  description = "Application Insights instrumentation key."
}

output "log_analytics_workspace_id" {
  value       = module.app_insights.log_analytics_workspace_id
  description = "Log Analytics workspace ID."
}
