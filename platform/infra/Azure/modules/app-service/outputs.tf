output "app_id" {
  description = "ID of the App Service."
  value       = azurerm_windows_web_app.this.id
}

output "default_hostname" {
  description = "Default hostname assigned to the App Service."
  value       = azurerm_windows_web_app.this.default_hostname
}

output "service_plan_id" {
  description = "ID of the App Service plan."
  value       = azurerm_service_plan.this.id
}
