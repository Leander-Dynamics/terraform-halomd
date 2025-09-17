# Environment composition for the dev environment

locals {
  rg_name                               = "rg-${var.project_name}-${var.env_name}"
  acr_name                              = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name                              = "aks-${var.project_name}-${var.env_name}"
  kv_name                               = "kv-${var.project_name}-${var.env_name}"
  log_name                              = "log-${var.project_name}-${var.env_name}"
  appi_name                             = var.application_insights_name != "" ? var.application_insights_name : "appi-${var.project_name}-${var.env_name}"
  plan_name                             = "asp-${var.project_name}-${var.env_name}"
  func_cron_name                        = "func-cron-${var.project_name}-${var.env_name}"
  func_external_name                    = "func-ext-${var.project_name}-${var.env_name}"
  web_name                              = "web-${var.project_name}-${var.env_name}"
  app_gateway_name                      = "agw-${var.project_name}-${var.env_name}"
  arbitration_plan_name                 = "asp-${var.project_name}-${var.env_name}-arb"
  arbitration_app_name                  = "web-${var.project_name}-${var.env_name}-arb"
  arbitration_plan_sku_effective        = var.arbitration_plan_sku != "" ? trimspace(var.arbitration_plan_sku) : "B1"
  arbitration_runtime_stack_effective   = var.arbitration_runtime_stack != "" ? trimspace(var.arbitration_runtime_stack) : "dotnet"
  arbitration_runtime_version_effective = var.arbitration_runtime_version != "" ? trimspace(var.arbitration_runtime_version) : "8.0"
  storage_data_name                     = lower(replace("st${var.project_name}${var.env_name}data", "-", ""))
  sql_server_name                       = "sql-${var.project_name}-${var.env_name}"
  aad_app_display                       = "aad-${var.project_name}-${var.env_name}"
}

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

module "acr" {
  count               = var.enable_acr ? 1 : 0
  source              = "../../Azure/modules/acr"
  name                = local.acr_name
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = var.tags
}

module "app_service" {
  source              = "../../Azure/modules/app-service"
  plan_name           = local.plan_name
  plan_sku            = var.plan_sku
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

module "app_insights" {
  source = "../../Azure/modules/app-insights"

  log_analytics_workspace_name = var.log_analytics_workspace_name
  application_insights_name    = var.application_insights_name

  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.tags
}

module "app_service_arbitration" {
  source    = "../../Azure/modules/app-service-arbitration"
  name      = local.arbitration_app_name
  plan_name = local.arbitration_plan_name
  plan_sku  = local.arbitration_plan_sku_effective
  #plan_os_type                   = var.app_service_plan_os_type
  resource_group_name            = module.resource_group.name
  location                       = var.location
  runtime_stack                  = local.arbitration_runtime_stack_effective
  runtime_version                = local.arbitration_runtime_version_effective
  app_insights_connection_string = module.app_insights.application_insights_connection_string
  log_analytics_workspace_id     = module.app_insights.log_analytics_workspace_id
  connection_strings             = var.arbitration_connection_strings
  app_settings                   = var.arbitration_app_settings
  tags                           = var.tags
}

locals {
  default_app_gateway_backend_fqdns = compact([
    module.app_service.default_hostname,
    module.app_service_arbitration.default_hostname,
  ])

  app_gateway_backend_fqdns = distinct(compact(concat(
    var.app_gateway_backend_fqdns,
    local.default_app_gateway_backend_fqdns,
  )))

  dns_hostname_overrides = {
    for hostname, replacement in {
      format("%s.azurewebsites.net", var.app_service_fqdn_prefix) = module.app_service.default_hostname
      format("%s.azurewebsites.net", local.arbitration_app_name)  = module.app_service_arbitration.default_hostname
    } : lower(hostname) => replacement
    if replacement != null && replacement != ""
  }

  dns_cname_records = {
    for name, cfg in var.dns_cname_records :
    name => merge(cfg, {
      record = lookup(local.dns_hostname_overrides, lower(cfg.record), cfg.record)
    })
  }
}

module "app_gateway" {
  source                              = "../../Azure/modules/app-gateway"
  name                                = local.app_gateway_name
  resource_group_name                 = module.resource_group.name
  location                            = var.location
  subnet_id                           = module.network.subnet_ids[var.app_gateway_subnet_key]
  fqdn_prefix                         = var.app_gateway_fqdn_prefix
  backend_fqdns                       = local.app_gateway_backend_fqdns
  backend_port                        = var.app_gateway_backend_port
  frontend_port                       = var.app_gateway_frontend_port
  listener_protocol                   = var.app_gateway_listener_protocol
  sku_name                            = var.app_gateway_sku_name
  sku_tier                            = var.app_gateway_sku_tier
  sku_capacity                        = var.app_gateway_capacity
  enable_http2                        = var.app_gateway_enable_http2
  pick_host_name_from_backend_address = var.app_gateway_pick_host_name
  tags                                = var.tags
}

module "sql" {
  count                         = var.enable_sql && var.sql_admin_login != "" && var.sql_admin_password != "" ? 1 : 0
  source                        = "../../Azure/modules/sql-serverless"
  server_name                   = local.sql_server_name
  database_name                 = var.sql_database_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  administrator_login           = var.sql_admin_login
  administrator_password        = var.sql_admin_password
  public_network_access_enabled = var.sql_public_network_access
  sku_name                      = var.sql_sku_name
  auto_pause_delay_in_minutes   = var.sql_auto_pause_delay
  max_size_gb                   = var.sql_max_size_gb
  min_capacity                  = var.sql_min_capacity
  max_capacity                  = var.sql_max_capacity
  read_scale                    = var.sql_read_scale
  zone_redundant                = var.sql_zone_redundant
  collation                     = var.sql_collation
  minimum_tls_version           = var.sql_minimum_tls_version
  firewall_rules                = var.sql_firewall_rules
  tags                          = var.tags
}

module "aad_app" {
  source       = "../../Azure/modules/aad-app"
  display_name = local.aad_app_display
}

module "kv" {
  source                        = "../../Azure/modules/key-vault"
  name                          = local.kv_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  public_network_access_enabled = var.kv_public_network_access
  tags                          = var.tags
  secrets = {
    "arbitration-storage-connection" = module.arbitration_storage_account.primary_connection_string
  }
}

module "dns_zone" {
  source              = "../../Azure/modules/dns-zone"
  zone_name           = var.dns_zone_name
  resource_group_name = module.resource_group.name
  tags                = var.tags
  a_records           = var.dns_a_records
  cname_records       = local.dns_cname_records
}

# ----------------------
# Outputs
# ----------------------

output "resource_group_name" {
  description = "Resource group provisioned for the environment."
  value       = module.resource_group.name
}

output "virtual_network_id" {
  description = "ID of the deployed virtual network."
  value       = module.network.virtual_network_id
}

output "app_service_default_hostname" {
  description = "Default hostname assigned to the primary App Service."
  value       = module.app_service.default_hostname
}

output "arbitration_app_service_default_hostname" {
  description = "Default hostname assigned to the arbitration App Service."
  value       = module.app_service_arbitration.default_hostname
}

output "app_gateway_id" {
  description = "ID of the Application Gateway."
  value       = module.app_gateway.id
}

output "app_gateway_public_ip_address" {
  description = "Allocated public IP address of the Application Gateway."
  value       = module.app_gateway.public_ip_address
}

output "app_gateway_public_fqdn" {
  description = "Public FQDN assigned to the Application Gateway."
  value       = module.app_gateway.public_ip_fqdn
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL Server."
  value       = length(module.sql) > 0 ? module.sql[0].server_fqdn : null
}

output "log_analytics_workspace_name" {
  description = "Name of the Log Analytics Workspace used in this environment."
  value       = var.log_analytics_workspace_name
}
