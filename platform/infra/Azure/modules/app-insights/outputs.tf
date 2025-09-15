output "application_insights_id" {
  description = "Resource ID of Application Insights."
  value       = azurerm_application_insights.this.id
}

output "application_insights_connection_string" {
  description = "Connection string used by applications to send telemetry."
  value       = azurerm_application_insights.this.connection_string
}

output "application_insights_instrumentation_key" {
  description = "Instrumentation key for Application Insights."
  value       = azurerm_application_insights.this.instrumentation_key
}

output "log_analytics_workspace_id" {
  description = "Resource ID of the Log Analytics workspace."
  value       = azurerm_log_analytics_workspace.this.id
}

output "log_analytics_workspace_customer_id" {
  description = "Workspace (customer) ID used by agents."
  value       = azurerm_log_analytics_workspace.this.workspace_id
}

output "log_analytics_primary_shared_key" {
  description = "Primary shared key for the Log Analytics workspace."
  value       = azurerm_log_analytics_workspace.this.primary_shared_key
  sensitive   = true
}
