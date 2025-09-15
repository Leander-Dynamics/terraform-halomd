output "app_id" {
  description = "Resource ID of the App Service."
  value       = azurerm_linux_web_app.this.id
}

output "default_hostname" {
  description = "Default hostname of the App Service."
  value       = azurerm_linux_web_app.this.default_hostname
}

output "principal_id" {
  description = "Principal ID of the managed identity."
  value       = azurerm_linux_web_app.this.identity[0].principal_id
}

output "service_plan_id" {
  description = "Resource ID of the App Service plan."
  value       = azurerm_service_plan.this.id
}
