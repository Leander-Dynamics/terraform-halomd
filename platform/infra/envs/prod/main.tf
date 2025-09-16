# Environment composition for the prod environment

locals {
  rg_name          = "rg-${var.project_name}-${var.env_name}"
  kv_name          = "kv-${var.project_name}-${var.env_name}"
  log_name         = var.log_analytics_workspace_name
  appi_name        = var.application_insights_name

  acr_name         = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name         = "aks-${var.project_name}-${var.env_name}-${var.location}"

  web_plan         = "asp-halomdweb-${var.env_name}-${var.location}"
  web_name         = "app-halomdweb-${var.env_name}"
  app_gateway_name = "agw-${var.project_name}-${var.env_name}"
  bastion_name     = "bas-${var.project_name}-${var.env_name}"
  arbitration_plan = "asp-${var.project_name}-arb-${var.env_name}-${var.location}"
  arbitration_name = "app-${var.project_name}-arb-${var.env_name}"

  func_external_plan = "asp-external-${var.env_name}-${var.location}"
  func_external_name = "func-external-${var.env_name}"
  func_cron_plan     = "asp-cron-${var.env_name}-${var.location}"
  func_cron_name     = "func-cron-${var.env_name}"

  sql_server_name   = "sql-${var.project_name}-${var.env_name}"
  sql_database_name = var.sql_database_name != "" ? var.sql_database_name : "${var.project_name}-${var.env_name}"

  subnet_network_security_groups = {
    for subnet_name in keys(var.subnets) :
    subnet_name => {
      name           = "nsg-${var.project_name}-${var.env_name}-${subnet_name}"
      security_rules = lookup(var.subnet_network_security_rules, subnet_name, {})
    }
  }

  kv_private_endpoint_name      = "pep-${var.project_name}-${var.env_name}-kv"
  storage_private_endpoint_name = "pep-${var.project_name}-${var.env_name}-st"

  nat_gateway_settings = var.enable_nat_gateway && var.nat_gateway_configuration != null ? {
    name                     = var.nat_gateway_configuration.name
    sku_name                 = try(var.nat_gateway_configuration.sku_name, "Standard")
    idle_timeout_in_minutes = try(var.nat_gateway_configuration.idle_timeout_in_minutes, 4)
    zones                    = try(var.nat_gateway_configuration.zones, [])
    public_ip_configurations = try(var.nat_gateway_configuration.public_ip_configurations, [])
    public_ip_ids            = try(var.nat_gateway_configuration.public_ip_ids, [])
    subnet_keys              = var.nat_gateway_configuration.subnet_keys
    tags                     = try(var.nat_gateway_configuration.tags, {})
  } : null

  nat_gateway_subnet_ids = local.nat_gateway_settings != null ? [
    for key in local.nat_gateway_settings.subnet_keys : module.network.subnet_ids[key]
  ] : []

  vpn_gateway_settings = var.enable_vpn_gateway && var.vpn_gateway_configuration != null ? {
    name                     = var.vpn_gateway_configuration.name
    gateway_subnet_key       = var.vpn_gateway_configuration.gateway_subnet_key
    sku                      = var.vpn_gateway_configuration.sku
    gateway_type             = try(var.vpn_gateway_configuration.gateway_type, "Vpn")
    vpn_type                 = try(var.vpn_gateway_configuration.vpn_type, "RouteBased")
    active_active            = try(var.vpn_gateway_configuration.active_active, false)
    enable_bgp               = try(var.vpn_gateway_configuration.enable_bgp, false)
    generation               = try(var.vpn_gateway_configuration.generation, null)
    ip_configuration_name    = try(var.vpn_gateway_configuration.ip_configuration_name, "default")
    custom_routes            = try(var.vpn_gateway_configuration.custom_routes, [])
    public_ip                = try(var.vpn_gateway_configuration.public_ip, null)
    public_ip_id             = try(var.vpn_gateway_configuration.public_ip_id, null)
    vpn_client_configuration = try(var.vpn_gateway_configuration.vpn_client_configuration, null)
    bgp_settings             = try(var.vpn_gateway_configuration.bgp_settings, null)
    tags                     = try(var.vpn_gateway_configuration.tags, {})
  } : null

  vpn_gateway_subnet_id = local.vpn_gateway_settings != null ? module.network.subnet_ids[local.vpn_gateway_settings.gateway_subnet_key] : null

  kv_private_endpoint_subnet_id = var.enable_kv_private_endpoint && var.kv_private_endpoint_subnet_key != null && var.kv_private_endpoint_subnet_key != "" ? lookup(module.network.subnet_ids, var.kv_private_endpoint_subnet_key, null) : null

  kv_private_endpoints = local.kv_private_endpoint_subnet_id != null ? [
    { subnet_id = local.kv_private_endpoint_subnet_id }
  ] : []

  storage_private_endpoint_subnet_id = var.enable_storage_private_endpoint && var.storage_private_endpoint_subnet_key != null && var.storage_private_endpoint_subnet_key != "" ? lookup(module.network.subnet_ids, var.storage_private_endpoint_subnet_key, null) : null
}

# -------------------------
# Core modules
# -------------------------

module "resource_group" {
  source   = "../../Azure/modules/resource-group"
  name     = local.rg_name
  location = var.location
  tags     = var.tags
}

module "app_insights" {
  source                           = "../../Azure/modules/app-insights"
  resource_group_name              = module.resource_group.name
  location                         = var.location
  log_analytics_workspace_name     = local.log_name
  application_insights_name        = local.appi_name
  log_analytics_retention_in_days  = var.log_analytics_retention_in_days
  log_analytics_daily_quota_gb     = var.log_analytics_daily_quota_gb
  tags                             = var.tags
}

module "dns_zone" {
  count               = length(trimspace(coalesce(var.dns_zone_name, ""))) > 0 ? 1 : 0
  source              = "../../Azure/modules/dns-zone"
  zone_name           = var.dns_zone_name
  resource_group_name = module.resource_group.name
  tags                = var.tags
  a_records           = var.dns_a_records
  cname_records       = var.dns_cname_records
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

module "network_security_groups" {
  for_each            = local.subnet_network_security_groups
  source              = "../../Azure/modules/network-security-group"
  name                = each.value.name
  resource_group_name = module.resource_group.name
  location            = var.location
  security_rules      = each.value.security_rules
  subnet_ids          = toset([module.network.subnet_ids[each.key]])
}

module "kv" {
  source                        = "../../Azure/modules/key-vault"
  name                          = local.kv_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  public_network_access_enabled = var.kv_public_network_access
  network_acls                  = var.kv_network_acls
  private_endpoints             = local.kv_private_endpoints
  tags                          = var.tags
}

module "kv_private_endpoint" {
  count = var.enable_kv_private_endpoint && local.kv_private_endpoint_subnet_id != null && coalesce(var.kv_private_endpoint_resource_id, module.kv.id) != null ? 1 : 0
  source              = "../../Azure/modules/private-endpoint"
  name                = local.kv_private_endpoint_name
  resource_group_name = module.resource_group.name
  location            = var.location
  subnet_id           = local.kv_private_endpoint_subnet_id
  tags                = var.tags

  private_service_connection = {
    name                           = "kv-${var.project_name}-${var.env_name}"
    private_connection_resource_id = coalesce(var.kv_private_endpoint_resource_id, module.kv.id)
    subresource_names              = ["vault"]
  }

  private_dns_zone_groups = length(var.kv_private_dns_zone_ids) > 0 ? [
    {
      name                 = "default"
      private_dns_zone_ids = var.kv_private_dns_zone_ids
    }
  ] : []
}

module "storage_private_endpoint" {
  count = var.enable_storage_private_endpoint && local.storage_private_endpoint_subnet_id != null && var.storage_account_private_connection_resource_id != null ? 1 : 0
  source              = "../../Azure/modules/private-endpoint"
  name                = local.storage_private_endpoint_name
  resource_group_name = module.resource_group.name
  location            = var.location
  subnet_id           = local.storage_private_endpoint_subnet_id
  tags                = var.tags

  private_service_connection = {
    name                           = "st-${var.project_name}-${var.env_name}"
    private_connection_resource_id = var.storage_account_private_connection_resource_id
    subresource_names              = var.storage_private_endpoint_subresource_names
  }

  private_dns_zone_groups = length(var.storage_private_dns_zone_ids) > 0 ? [
    {
      name                 = "default"
      private_dns_zone_ids = var.storage_private_dns_zone_ids
    }
  ] : []
}

module "app_service_web" {
  source = "../../Azure/modules/app-service-web"

  name                = local.web_name
  plan_name           = local.web_plan
  plan_sku            = var.app_service_plan_sku
  resource_group_name = module.resource_group.name
  location            = var.location

  dotnet_version                  = var.app_service_dotnet_version
  app_insights_connection_string = var.app_service_app_insights_connection_string
  log_analytics_workspace_id     = var.app_service_log_analytics_workspace_id
  app_settings                   = var.app_service_app_settings
  connection_strings             = var.app_service_connection_strings
  tags                           = var.tags
}

module "app_service_arbitration" {
  count = var.enable_arbitration_app_service ? 1 : 0
  source = "../../Azure/modules/app-service-arbitration"

  name                = local.arbitration_name
  plan_name           = local.arbitration_plan
  plan_sku            = var.arbitration_app_plan_sku != null && trimspace(var.arbitration_app_plan_sku) != "" ? var.arbitration_app_plan_sku : var.app_service_plan_sku
  resource_group_name = module.resource_group.name
  location            = var.location

  runtime_stack                  = var.arbitration_runtime_stack
  runtime_version                = var.arbitration_runtime_version
  app_insights_connection_string = var.arbitration_app_insights_connection_string != null && trimspace(var.arbitration_app_insights_connection_string) != "" ? var.arbitration_app_insights_connection_string : var.app_service_app_insights_connection_string
  log_analytics_workspace_id     = var.arbitration_log_analytics_workspace_id != null && trimspace(var.arbitration_log_analytics_workspace_id) != "" ? var.arbitration_log_analytics_workspace_id : var.app_service_log_analytics_workspace_id
  connection_strings             = var.arbitration_connection_strings
  app_settings                   = var.arbitration_app_settings
  run_from_package               = var.arbitration_run_from_package
  tags                           = var.tags
}

module "sql_serverless" {
  source                         = "../../Azure/modules/sql-serverless"
  server_name                    = local.sql_server_name
  database_name                  = local.sql_database_name
  resource_group_name            = module.resource_group.name
  location                       = var.location
  administrator_login            = var.sql_admin_login
  administrator_password         = var.sql_admin_password
  public_network_access_enabled  = var.sql_public_network_access
  sku_name                       = var.sql_sku_name
  max_size_gb                    = var.sql_max_size_gb
  auto_pause_delay_in_minutes    = var.sql_auto_pause_delay
  min_capacity                   = var.sql_min_capacity
  max_capacity                   = var.sql_max_capacity
  firewall_rules                 = var.sql_firewall_rules
  tags                           = var.tags
}

module "nat_gateway" {
  for_each = local.nat_gateway_settings == null ? {} : { default = local.nat_gateway_settings }
  source                  = "../../Azure/modules/nat-gateway"
  name                    = each.value.name
  resource_group_name     = module.resource_group.name
  location                = var.location
  sku_name                = each.value.sku_name
  idle_timeout_in_minutes = each.value.idle_timeout_in_minutes
  zones                   = each.value.zones
  public_ip_configurations = each.value.public_ip_configurations
  public_ip_ids            = each.value.public_ip_ids
  subnet_ids               = local.nat_gateway_subnet_ids
  tags                     = merge(var.tags, each.value.tags)
}

module "vpn_gateway" {
  for_each = local.vpn_gateway_settings == null ? {} : { default = local.vpn_gateway_settings }
  source                  = "../../Azure/modules/vpn-gateway"
  name                    = each.value.name
  resource_group_name     = module.resource_group.name
  location                = var.location
  gateway_subnet_id       = local.vpn_gateway_subnet_id
  gateway_type            = each.value.gateway_type
  sku                     = each.value.sku
  vpn_type                = each.value.vpn_type
  active_active           = each.value.active_active
  enable_bgp              = each.value.enable_bgp
  generation              = each.value.generation
  ip_configuration_name   = each.value.ip_configuration_name
  custom_route_address_prefixes = each.value.custom_routes
  public_ip_configuration      = each.value.public_ip
  public_ip_id                 = each.value.public_ip_id
  vpn_client_configuration     = each.value.vpn_client_configuration
  bgp_settings                 = each.value.bgp_settings
  tags                         = merge(var.tags, each.value.tags)
}

module "bastion" {
  count               = var.enable_bastion ? 1 : 0
  source              = "../../Azure/modules/bastion"
  name                = local.bastion_name
  resource_group_name = module.resource_group.name
  location            = var.location
  subnet_id           = var.enable_bastion ? module.network.subnet_ids[var.bastion_subnet_key] : null
  tags                = var.tags
}

# -------------------------
# Outputs
# -------------------------
output "nsg_ids" {
  description = "IDs of deployed Network Security Groups."
  value       = { for k, v in module.network_security_groups : k => v.id }
}

output "kv_private_endpoint_id" {
  description = "Resource ID of the Key Vault private endpoint."
  value       = try(module.kv_private_endpoint[0].id, null)
}

output "storage_private_endpoint_id" {
  description = "Resource ID of the Storage private endpoint."
  value       = try(module.storage_private_endpoint[0].id, null)
}

output "nat_gateway_id" {
  description = "Resource ID of the NAT Gateway when provisioned."
  value       = try(module.nat_gateway["default"].id, null)
}

output "vpn_gateway_id" {
  description = "Resource ID of the virtual network gateway when provisioned."
  value       = try(module.vpn_gateway["default"].id, null)
}

output "bastion_host_id" {
  description = "Resource ID of the Bastion host."
  value       = var.enable_bastion ? module.bastion[0].id : null
}

output "bastion_public_ip_address" {
  description = "Public IP address associated with the Bastion host."
  value       = var.enable_bastion ? module.bastion[0].public_ip_address : null
}
