output "app_insights_connection_string" { value = azurerm_application_insights.appi.connection_string }
output "log_analytics_workspace_id"     { value = azurerm_log_analytics_workspace.law.id }
