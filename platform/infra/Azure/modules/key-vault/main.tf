data "azurerm_client_config" "current" {}

locals {
  network_acls_input = var.network_acls != null ? var.network_acls : {}

  private_endpoint_subnet_ids = distinct(compact([
    for endpoint in var.private_endpoints : try(endpoint.subnet_id, null)
    if try(trimspace(endpoint.subnet_id), "") != ""
  ]))

  network_acls_effective = {
    bypass                     = try(local.network_acls_input.bypass, null)
    default_action             = try(local.network_acls_input.default_action, null)
    ip_rules                   = try(local.network_acls_input.ip_rules, [])
    virtual_network_subnet_ids = distinct(compact(concat(
      try(local.network_acls_input.virtual_network_subnet_ids, []),
      local.private_endpoint_subnet_ids,
    )))
  }

  should_define_network_acls = (
    var.network_acls != null ||
    length(local.network_acls_effective.virtual_network_subnet_ids) > 0 ||
    length(local.network_acls_effective.ip_rules) > 0 ||
    local.network_acls_effective.bypass != null ||
    local.network_acls_effective.default_action != null
  )
}

resource "azurerm_key_vault" "kv" {
  name                          = var.name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  sku_name                      = "standard"
  purge_protection_enabled      = true
  soft_delete_retention_days    = 90
  public_network_access_enabled = var.public_network_access_enabled
  tags                          = var.tags

  dynamic "network_acls" {
    for_each = local.should_define_network_acls ? [local.network_acls_effective] : []

    content {
      bypass                     = network_acls.value.bypass
      default_action             = network_acls.value.default_action
      ip_rules                   = network_acls.value.ip_rules
      virtual_network_subnet_ids = network_acls.value.virtual_network_subnet_ids
    }
  }
}
