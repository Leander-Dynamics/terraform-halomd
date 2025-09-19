resource "azurerm_storage_account" "this" {
  name                     = var.name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = var.account_tier
  account_replication_type = var.account_replication_type

  # SFTP implementation
  sftp_enabled = var.sftp_enabled
  is_hns_enabled = var.hns_enabled
}
