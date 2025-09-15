locals {
  name_prefix         = "${var.project_name}-${var.env_name}"
  tags                = merge(var.tags, { Environment = var.env_name })
  resource_group_name = coalesce(var.resource_group_name, "rg-${local.name_prefix}")
  network_name        = coalesce(var.network_name, "vnet-${local.name_prefix}")
  app_service_plan    = coalesce(var.app_service_plan_name, "asp-${local.name_prefix}")
  app_service_name    = coalesce(var.app_service_name, "web-${local.name_prefix}")
  sql_server_name     = coalesce(var.sql_server_name, "sql-${local.name_prefix}")
  sql_database_name   = coalesce(var.sql_database_name, "${var.project_name}-${var.env_name}")
}

module "resource_group" {
  source   = "../../Azure/modules/resource-group"
  name     = local.resource_group_name
  location = var.location
  tags     = local.tags
}

module "network" {
  count               = var.network_enabled ? 1 : 0
  source              = "../../Azure/modules/network"
  name                = local.network_name
  resource_group_name = module.resource_group.name
  location            = var.location
  address_space       = var.network_address_space
  subnets             = var.network_subnets
  tags                = local.tags
}

module "dns" {
  count               = var.dns_enabled ? 1 : 0
  source              = "../../Azure/modules/dns"
  zone_name           = var.dns_zone_name
  resource_group_name = module.resource_group.name
  tags                = local.tags
  a_records           = var.dns_a_records
  cname_records       = var.dns_cname_records
}

module "app_service" {
  count                          = var.app_service_enabled ? 1 : 0
  source                         = "../../Azure/modules/app-service"
  name                           = local.app_service_name
  plan_name                      = local.app_service_plan
  resource_group_name            = module.resource_group.name
  location                       = var.location
  plan_sku                       = var.app_service_plan_sku
  dotnet_version                 = var.app_service_dotnet_version
  app_insights_connection_string = var.app_service_app_insights_connection_string
  log_analytics_workspace_id     = var.app_service_log_analytics_workspace_id
  app_settings                   = var.app_service_app_settings
  connection_strings             = var.app_service_connection_strings
  https_only                     = var.app_service_https_only
  always_on                      = var.app_service_always_on
  identity_type                  = var.app_service_identity_type
  tags                           = local.tags
}

module "sql" {
  count                         = var.sql_enabled ? 1 : 0
  source                        = "../../Azure/modules/sql"
  server_name                   = local.sql_server_name
  db_name                       = local.sql_database_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  admin_login                   = var.sql_admin_login
  admin_password                = var.sql_admin_password
  minimum_tls_version           = var.sql_minimum_tls_version
  public_network_access_enabled = var.sql_public_network_access_enabled
  sku_name                      = var.sql_sku_name
  max_size_gb                   = var.sql_max_size_gb
  min_capacity                  = var.sql_min_capacity
  auto_pause_delay_in_minutes   = var.sql_auto_pause_delay_in_minutes
  zone_redundant                = var.sql_zone_redundant
  backup_storage_redundancy     = var.sql_backup_storage_redundancy
  firewall_rules                = var.sql_firewall_rules
  tags                          = local.tags
}

output "resource_group_name" {
  description = "Name of the resource group created for the environment."
  value       = module.resource_group.name
}

output "virtual_network_id" {
  description = "Resource ID of the virtual network (when enabled)."
  value       = var.network_enabled ? module.network[0].id : null
}

output "subnet_ids" {
  description = "Map of subnet names to their IDs (when the network module is enabled)."
  value       = var.network_enabled ? module.network[0].subnet_ids : {}
}

output "dns_zone_name" {
  description = "DNS zone managed by this environment."
  value       = var.dns_enabled ? module.dns[0].zone_name : null
}

output "app_service_default_hostname" {
  description = "Hostname of the App Service when the module is enabled."
  value       = var.app_service_enabled ? module.app_service[0].default_hostname : null
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL server when enabled."
  value       = var.sql_enabled ? module.sql[0].server_fqdn : null
}
