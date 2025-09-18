
resource "azurerm_mssql_server" "workflow_sqlserver" {
    name                          = "${var.env_region}-arbit-workflow-appplication-sqlserver"
    resource_group_name           = azurerm_resource_group.workflow_rg.name
    location                      = azurerm_resource_group.workflow_rg.location
    version                       = "12.0"
    administrator_login           = "dbadmin"
    administrator_login_password  = var.workflow_sqlserver_dbadmin_password
    public_network_access_enabled = false

    azuread_administrator {
      azuread_authentication_only = false
      login_username              = "SQL Admins"
      object_id                   = "b846eec0-b0b9-40d4-a1e3-2fbaa8e83905"
      tenant_id                   = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"
    }
}

resource "azurerm_mssql_database" "workflow_app_db" {
    name           = "${var.env_region}-arbit-workflow-app-db"
    server_id      = azurerm_mssql_server.workflow_sqlserver.id
    collation      = "SQL_Latin1_General_CP1_CI_AS"
    license_type   = "LicenseIncluded"
    max_size_gb    = 1
    sku_name       = "Basic"
}

resource "azurerm_mssql_database" "workflow_logs_db" {
    name           = "${var.env_region}-arbit-workflow-logs-db"
    server_id      = azurerm_mssql_server.workflow_sqlserver.id
    collation      = "SQL_Latin1_General_CP1_CI_AS"
    license_type   = "LicenseIncluded"
    max_size_gb    = 1
    sku_name       = "Basic"
}

## cant do this right now because of subnet,
# Allow access to the SQL server from the private asps subnet where the app services reside
# resource "azurerm_mssql_virtual_network_rule" "workflow_db_network_rule" {
#   name                = "${var.env_region}-arbit-workflow-app-vnet-rule-1"
#   server_id           = azurerm_mssql_server.workflow_sqlserver.id
#   subnet_id           =  "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-asps-snet-1"
#   ignore_missing_vnet_service_endpoint = false
# }
