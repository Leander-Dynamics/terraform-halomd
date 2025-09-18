# Storage account for arbit workflow application - docs
resource "azurerm_storage_account" "workflow_storage_account_docs" {
  name                     = var.workflow_storage_account_docs
  resource_group_name      = azurerm_resource_group.workflow_rg.name
  location                 = azurerm_resource_group.workflow_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  is_hns_enabled           = true
}

# Storage account for arbit workflow application - cron functions
resource "azurerm_storage_account" "workflow_storage_account_cron_function" {
  name                     = var.workflow_storage_account_cron_function
  resource_group_name      = azurerm_resource_group.workflow_rg.name
  location                 = azurerm_resource_group.workflow_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  is_hns_enabled           = true
}

resource "azurerm_storage_account" "workflow_storage_account_external_function" {
  name                     = var.workflow_storage_account_external_function
  resource_group_name      = azurerm_resource_group.workflow_rg.name
  location                 = azurerm_resource_group.workflow_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  is_hns_enabled           = true
}

# container creation example
# resource "azurerm_storage_container" "workflow_storage_account_docs_container" {
#   name                  = "backup"
#   storage_account_id    = azurerm_storage_account.workflow_storage_account_docs.id
#   container_access_type = "private"
# }
