output "server_name"  { value = azurerm_mssql_server.server.name }
output "server_fqdn"  { value = "${azurerm_mssql_server.server.name}.database.windows.net" }
output "database_id"  { value = azurerm_mssql_database.db.id }
