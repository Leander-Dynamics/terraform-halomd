resource "azurerm_service_plan" "plan" {
  name                = var.plan_name
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.plan_sku
  tags                = var.tags
}
resource "azurerm_linux_web_app" "app" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = true
  identity { type = "SystemAssigned" }
  site_config {
    ftps_state = "Disabled"
    application_stack { dotnet_version = var.dotnet_version }
  }
  app_settings = merge({
    "APPINSIGHTS_CONNECTION_STRING" = var.app_insights_connection_string
    "WEBSITE_RUN_FROM_PACKAGE"      = "1"
  }, var.app_settings)
  tags = var.tags
}

resource "azurerm_monitor_diagnostic_setting" "app" {
  count = var.log_analytics_workspace_id == null || var.log_analytics_workspace_id == "" ? 0 : 1

  name                       = "${var.name}-diag"
  target_resource_id         = azurerm_linux_web_app.app.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  dynamic "log" {
    for_each = [
      "AppServiceHTTPLogs",
      "AppServiceConsoleLogs",
      "AppServiceAppLogs",
      "AppServiceAuditLogs",
      "AppServiceFileAuditLogs",
      "AppServicePlatformLogs",
    ]
    content {
      category = log.value
      enabled  = true

      retention_policy {
        enabled = false
      }
    }
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      enabled = false
    }
  }
}
