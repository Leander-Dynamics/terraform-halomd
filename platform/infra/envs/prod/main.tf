# Environment composition for the prod environment

locals {
  rg_name          = "rg-${var.project_name}-${var.env_name}"
  kv_name          = "kv-${var.project_name}-${var.env_name}"
  log_name         = "log-${var.project_name}-${var.env_name}"
  appi_name        = "appi-${var.project_name}-${var.env_name}"

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

modul
