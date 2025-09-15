output "id" {
  description = "Resource ID of the Linux Function App."
  value       = azurerm_linux_function_app.this.id
}

output "name" {
  description = "Name of the Linux Function App."
  value       = azurerm_linux_function_app.this.name
}

output "default_hostname" {
  description = "Default hostname assigned to the Function App."
  value       = azurerm_linux_function_app.this.default_hostname
}

output "principal_id" {
  description = "Principal ID of the system-assigned managed identity."
  value       = azurerm_linux_function_app.this.identity[0].principal_id
}

output "service_plan_id" {
  description = "ID of the App Service plan hosting the Function App."
  value       = azurerm_service_plan.this.id
}

output "storage_account_name" {
  description = "Name of the storage account backing the Function App."
  value       = azurerm_storage_account.this.name
}
