output "name" {
  description = "Name of the deployed web app."
  value       = azurerm_linux_web_app.app.name
}

output "default_hostname" {
  description = "Default hostname assigned to the web app."
  value       = azurerm_linux_web_app.app.default_hostname
}

output "service_plan_id" {
  description = "Resource ID of the App Service plan."
  value       = azurerm_service_plan.plan.id
}

output "principal_id" {
  description = "Managed identity principal ID for the web app."
  value       = azurerm_linux_web_app.app.identity[0].principal_id
}
