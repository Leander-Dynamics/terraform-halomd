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
  bastion_name         = "bas-${var.project_name}-${var.env_name}"
  arbitration_plan_name = "asp-${var.project_name}-${var.env_name}-arb"
  arbitration_app_name  = "web-${var.project_name}-${var.env_name}-arb"
  arbitration_plan_sku_effective       = coalesce(nullif(trimspace(coalesce(var.arbitration_plan_sku, "")), ""), "B1")
  arbitration_runtime_stack_effective  = coalesce(nullif(trimspace(coalesce(var.arbitration_runtime_stack, "")), ""), "dotnet")
  arbitration_runtime_version_effective = coalesce(nullif(trimspace(coalesce(var.arbitration_runtime_version, "")), ""), "8.0")
  storage_data_name    = lower(replace("st${var.project_name}${var.env_name}data", "-", ""))
  sql_server_name      = "sql-${var.project_name}-${var.env_name}"
  aad_app_display      = "aad-${var.project_name}-${var.env_name}"

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

module "bastion" {
  count               = var.enable_bastion ? 1 : 0
  source              = "../../Azure/modules/bastion"
  name                = local.bastion_name
  resource_group_name = module.resource_group.name
  location            = var.location
  subnet_id           = var.enable_bastion ? module.network.subnet_ids[var.bastion_subnet_key] : null
  tags                = var.tags
}

# ... rest of the modules unchanged (acr, app_service, app_insights, arbitration, app_gateway, sql, aad_app, kv, dns_zone, outputs) ...

# ----------------------
# Outputs
# ----------------------

output "nat_gateway_id" {
  description = "Resource ID of the NAT Gateway when provisioned."
  value       = try(module.nat_gateway["default"].id, null)
}

output "nat_gateway_public_ip_ids" {
  description = "Public IP resource IDs attached to the NAT Gateway."
  value       = try(module.nat_gateway["default"].public_ip_ids, [])
}

output "vpn_gateway_id" {
  description = "Resource ID of the virtual network gateway when provisioned."
  value       = try(module.vpn_gateway["default"].id, null)
}

output "vpn_gateway_public_ip_id" {
  description = "Public IP resource ID associated with the virtual network gateway."
  value       = try(module.vpn_gateway["default"].public_ip_id, null)
}

output "bastion_host_id" {
  description = "Resource ID of the Bastion host."
  value       = var.enable_bastion ? module.bastion[0].id : null
}

output "bastion_public_ip_address" {
  description = "Public IP address associated with the Bastion host."
  value       = var.enable_bastion ? module.bastion[0].public_ip_address : null
}
