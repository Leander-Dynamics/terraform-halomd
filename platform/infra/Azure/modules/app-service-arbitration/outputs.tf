output "name" {
  value = azurerm_windows_web_app.this.name
}

output "default_hostname" {
  value = azurerm_windows_web_app.this.default_hostname
}

output "service_plan_id" {
  value = azurerm_service_plan.this.id
}

output "principal_id" {
  value = azurerm_windows_web_app.this.identity[0].principal_id
}
