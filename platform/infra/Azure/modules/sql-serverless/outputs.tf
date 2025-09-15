output "server_id" {
  description = "ID of the SQL Server."
  value       = azurerm_mssql_server.this.id
}

output "server_name" {
  description = "Name of the SQL Server."
  value       = azurerm_mssql_server.this.name
}

output "server_fqdn" {
  description = "Fully qualified domain name of the SQL Server."
  value       = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "database_id" {
  description = "ID of the SQL Database."
  value       = azurerm_mssql_database.this.id
}

output "database_name" {
  description = "Name of the SQL Database."
  value       = azurerm_mssql_database.this.name
}
