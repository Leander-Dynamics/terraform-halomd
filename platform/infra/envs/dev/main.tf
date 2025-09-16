# Environment composition for the dev environment

locals {
  rg_name                      = "rg-${var.project_name}-${var.env_name}"
  kv_name                      = "kv-${var.project_name}-${var.env_name}"
  bastion_name                 = "bas-${var.project_name}-${var.env_name}"
  log_name                     = var.log_analytics_workspace_name
  appi_name                    = var.application_insights_name

  kv_private_endpoint_name      = "pep-${var.project_name}-${var.env_name}-kv"
  storage_private_endpoint_name = "pep-${var.project_name}-${var.env_name}-st"

  # App Service naming
  app_service_plan_name         = "asp-${var.project_name}-web-${var.env_name}-${var.location}"
  app_service_name              = "app-${var.project_name}-web-${var.env_name}"
  arbitration_plan_name         = "asp-${var.project_name}-arb-${var.env_name}-${var.location}"
  arbitration_app_name          = "app-${var.project_name}-arb-${var.env_name}"

  # SQL Server
  sql_server_name               = "sql-${var.project_name}-${var.env_name}"
  sql_database_name             = var.sql_database_name != "" ? var.sql_database_name : "${var.project_name}-${var.env_name}"

  # NSG locals
  subnet_network_security_groups = {
    for subnet_name in keys(var.subnets) :
    subnet_name => {
      name           = "nsg-${var.project_name}-${var.env_name}-${subnet_name}"
      security_rules = lookup(var.subnet_network_security_rules, subnet_name, {})
    }
  }

  # NAT Gateway
  nat_gateway_settings = var.enable_nat_gateway && var.nat_gateway_configuration != null ? {
    name                     = var.nat_gateway_configuration.name
    sku_name                 = try(var.nat_gateway_configuration.sku_name, "Standard")
    idle_timeout_in_minutes  = try(var.nat_gateway_configuration.idle_timeout_in_minutes, 4)
    zones                    = try(var.nat_gateway_configuration.zones, [])
    public_ip_configurations = try(var.nat_gateway_configuration.public_ip_configurations, [])
    public_ip_ids            = try(var.nat_gateway_configuration.public_ip_ids, [])
    subnet_keys              = var.nat_gateway_configuration.subnet_keys
    tags                     = try(var.nat_gateway_configuration.tags, {})
  } : null

  nat_gateway_subnet_ids = local.nat_gateway_settings != null ? [
    for key in local.nat_gateway_settings.subnet_keys : module.network.subnet_ids[key]
  ] : []

  # VPN Gateway
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

  # Private Endpoints
  kv_private_endpoint_subnet_id = var.enable_kv_private_endpoint && var.kv_private_endpoint_subnet_key != null && var.kv_private_endpoint_subnet_key != "" ? lookup(module.network.subnet_ids, var.kv_private_endpoint_subnet_key, null) : null
  kv_private_endpoints = local.kv_private_endpoint_subnet_id != null ? [{ subnet_id = local.kv_private_endpoint_subnet_id }] : []

  storage_private_endpoint_subnet_id = var.enable_storage_private_endpoint && var.storage_private_endpoint_subnet_key != null && var.storage_private_endpoint_subnet_key != "" ? lookup(module.network.subnet_ids, var.storage_private_endpoint_subnet_key, null) : null
  storage_private_endpoints = local.storage_private_endpoint_subnet_id != null ? [{ subnet_id = local.storage_private_endpoint_subnet_id }] : []
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

  private_dns_zone_groups = length(var.kv_private_dns_zone_ids) > 0 ? [{
    name                 = "default"
    private_dns_zone_ids = var.kv_private_dns_zone_ids
  }] : []
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

  private_dns_zone_groups = length(var.storage_private_dns_zone_ids) > 0 ? [{
    name                 = "default"
    private_dns_zone_ids = var.storage_private_dns_zone_ids
  }] : []
}

# App Services
module "app_service_web" {
  source = "../../Azure/modules/app-service-web"

  name                = local.app_service_name
  plan_name           = local.app_service_plan_name
  plan_sku            = var.app_service_plan_sku
  resource_group_name = module.resource_group.name
  location            = var.location

  dotnet_version                  = var.app_service_dotnet_version
  app_insights_connection_string = var.app_service_app_insights_connection_string
  log_analytics_workspace_id     = var.app_service_log_analytics_workspace_id
  app_settings                   = var.app_service_app_settings
  connection_stri_
