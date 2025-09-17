data "azurerm_client_config" "current" {}

locals {
  filtered_secrets = {
    for name, cfg in var.secrets :
    name => {
      value        = cfg.value
      content_type = try(cfg.content_type, null)
      tags         = try(cfg.tags, null)
    }
    if try(trim(cfg.value), "") != ""
  }

  filtered_rbac_assignments = {
    for name, cfg in var.rbac_assignments :
    name => {
      principal_id         = cfg.principal_id
      role_definition_id   = try(cfg.role_definition_id, null)
      role_definition_name = try(cfg.role_definition_name, null)
    }
    if try(trim(cfg.principal_id), "") != ""
      && (try(cfg.role_definition_id, "") != "" || try(cfg.role_definition_name, "") != "")
  }
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
  enable_rbac_authorization     = var.enable_rbac_authorization
  tags                          = var.tags
}

resource "azurerm_key_vault_secret" "this" {
  for_each = local.filtered_secrets

  name         = each.key
  value        = each.value.value
  key_vault_id = azurerm_key_vault.kv.id
  content_type = each.value.content_type
  tags         = each.value.tags
}

resource "azurerm_role_assignment" "this" {
  for_each = local.filtered_rbac_assignments

  scope                = azurerm_key_vault.kv.id
  principal_id         = each.value.principal_id
  role_definition_id   = each.value.role_definition_id
  role_definition_name = each.value.role_definition_name
}
