resource "azurerm_mssql_server" "workflow" {
  name                          = local.names.sql_server
  resource_group_name           = module.resource_group.name
  location                      = var.region
  version                       = "12.0"
  administrator_login           = var.workflow_sqlserver_administrator_login
  administrator_login_password  = var.workflow_sqlserver_dbadmin_password
  public_network_access_enabled = false
  tags                          = var.tags

  azuread_administrator {
    azuread_authentication_only = false
    login_username              = var.sql_ad_admin_login_username
    object_id                   = var.sql_ad_admin_object_id
    tenant_id                   = var.sql_ad_admin_tenant_id
  }
}

resource "azurerm_mssql_database" "app" {
  name         = local.names.sql_app_db
  server_id    = azurerm_mssql_server.workflow.id
  collation    = "SQL_Latin1_General_CP1_CI_AS"
  license_type = "LicenseIncluded"
  max_size_gb  = 1
  sku_name     = "Basic"
}

resource "azurerm_mssql_database" "logs" {
  name         = local.names.sql_logs_db
  server_id    = azurerm_mssql_server.workflow.id
  collation    = "SQL_Latin1_General_CP1_CI_AS"
  license_type = "LicenseIncluded"
  max_size_gb  = 1
  sku_name     = "Basic"
}
