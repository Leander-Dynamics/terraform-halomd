output "id" {
  description = "Resource ID of the Linux App Service."
  value       = azurerm_linux_web_app.this.id
}

output "name" {
  description = "Name of the Linux App Service."
  value       = azurerm_linux_web_app.this.name
}

output "default_hostname" {
  description = "Default hostname assigned to the Linux App Service."
  value       = azurerm_linux_web_app.this.default_hostname
}

output "principal_id" {
  description = "System-assigned managed identity principal ID for the App Service."
  value       = azurerm_linux_web_app.this.identity[0].principal_id
}

output "service_plan_id" {
  description = "Resource ID of the associated App Service plan."
  value       = azurerm_service_plan.this.id
}
