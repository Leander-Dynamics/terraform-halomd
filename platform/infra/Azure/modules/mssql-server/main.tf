resource "azurerm_mssql_server" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  # administrator_login          = var.admin_login
  # administrator_login_password = var.admin_password
}
