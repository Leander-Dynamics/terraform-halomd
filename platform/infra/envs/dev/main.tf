# Environment composition for the dev environment

locals {
  rg_name              = "rg-${var.project_name}-${var.env_name}"
  acr_name             = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name             = "aks-${var.project_name}-${var.env_name}"
  kv_name              = "kv-${var.project_name}-${var.env_name}"
  log_name             = "log-${var.project_name}-${var.env_name}"
  appi_name            = var.app_insights_name != "" ? var.app_insights_name : "appi-${var.project_name}-${var.env_name}"
  plan_name            = "asp-${var.project_name}-${var.env_name}"
  func_cron_name       = "func-cron-${var.project_name}-${var.env_name}"
  func_external_name   = "func-ext-${var.project_name}-${var.env_name}"
  web_name             = "web-${var.project_name}-${var.env_name}"
  app_gateway_name     = "agw-${var.project_name}-${var.env_name}"
  arbitration_plan_name = "asp-${var.project_name}-${var.env_name}-arb"
  arbitration_app_name  = "web-${var.project_name}-${var.env_name}-arb"
  storage_data_name    = lower(replace("st${var.project_name}${var.env_name}data", "-", ""))
  sql_server_name      = "sql-${var.project_name}-${var.env_name}"
  aad_app_display      = "aad-${var.project_name}-${var.env_name}"
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

module "acr" {
  count               = var.enable_acr ? 1 : 0
  source              = "../../Azure/modules/acr"
  name                = local.acr_name
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = var.tags
}

module "app_gateway" {
  source              = "../../Azure/modules/app-gateway"
  name                = local.app_gateway_name
  resource_group_name = module.resource_group.name
  location            = var.location
  subnet_id           = module.network.subnet_ids[var.app_gateway_subnet_key]
  fqdn_prefix         = var.app_gateway_fqdn_prefix
  backend_fqdns       = var.app_gateway_backend_fqdns
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

module "sql" {
  count                         = var.enable_sql ? 1 : 0
  source                        = "../../Azure/modules/sql-serverless"
  server_name                   = local.sql_server_name
  database_name                 = var.sql_database_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  administrator_login           = var.sql_admin_login
  administrator_password        = var.sql_admin_password
  public_network_access_enabled = var.sql_public_network_access
  sku_name                      = var.sql_sku_name
  auto_pause_delay_in_minutes   = var.sql_auto_pause_minutes
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
}

module "app_insights" {
  source              = "../../Azure/modules/app-insights"
  resource_group_name = coalesce(var.app_insights_resource_group_name, module.resource_group.name)
  location            = var.location
  tags                = var.tags
}

module "dns_zone" {
  source              = "../../Azure/modules/dns-zone"
  zone_name           = var.dns_zone_name
  resource_group_name = module.resource_group.name
  tags                = var.tags
  a_records           = var.dns_a_records
  cname_records       = var.dns_cname_records
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
  value       = var.enable_sql ? module.sql[0].server_fqdn : null
}
