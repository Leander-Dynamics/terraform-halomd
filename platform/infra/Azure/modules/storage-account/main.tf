locals {
  network_rules_input = var.network_rules != null ? var.network_rules : {}

  private_endpoint_resource_ids = compact([
    for endpoint in var.private_endpoints : try(trimspace(endpoint.id), "")
    if try(trimspace(endpoint.id), "") != ""
  ])

  private_endpoint_subnet_ids = distinct(compact([
    for endpoint in var.private_endpoints : try(endpoint.subnet_id, null)
    if try(trimspace(endpoint.subnet_id), "") != ""
  ]))

  network_rules_effective = {
    bypass                     = try(local.network_rules_input.bypass, [])
    default_action             = try(local.network_rules_input.default_action, null)
    ip_rules                   = try(local.network_rules_input.ip_rules, [])
    virtual_network_subnet_ids = distinct(concat(
      try(local.network_rules_input.virtual_network_subnet_ids, []),
      local.private_endpoint_subnet_ids,
    ))
  }

  should_define_network_rules = (
    var.network_rules != null ||
    length(local.network_rules_effective.bypass) > 0 ||
    local.network_rules_effective.default_action != null ||
    length(local.network_rules_effective.ip_rules) > 0 ||
    length(local.network_rules_effective.virtual_network_subnet_ids) > 0 ||
    length(local.private_endpoint_resource_ids) > 0
  )
}

resource "azurerm_storage_account" "sa" {
  name                     = var.name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = var.account_tier
  account_replication_type = var.replication_type
  account_kind             = "StorageV2"
  # Toggle anonymous blob access based on module input.
  allow_blob_public_access = var.allow_blob_public_access
  is_hns_enabled           = var.enable_hns
  min_tls_version          = var.min_tls_version
  tags                     = var.tags

  dynamic "network_rules" {
    for_each = local.should_define_network_rules ? [local.network_rules_effective] : []

    content {
      bypass                     = network_rules.value.bypass
      default_action             = network_rules.value.default_action
      ip_rules                   = network_rules.value.ip_rules
      virtual_network_subnet_ids = network_rules.value.virtual_network_subnet_ids

      dynamic "private_link_access" {
        for_each = local.private_endpoint_resource_ids

        content {
          endpoint_resource_id = private_link_access.value
        }
      }
    }
  }
}
