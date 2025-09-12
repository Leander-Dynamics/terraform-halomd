resource "azurerm_storage_container" "this" {
  name                  = var.name
  container_access_type = var.access_type
}
