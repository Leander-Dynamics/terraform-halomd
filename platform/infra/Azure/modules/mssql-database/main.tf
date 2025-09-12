resource "azurerm_mssql_database" "this" {
  name        = var.name
  server_id   = var.server_id
  sku_name    = var.sku_name
}
