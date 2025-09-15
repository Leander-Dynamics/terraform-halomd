resource "azurerm_service_plan" "this" {
  name                = var.plan_name
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.plan_sku
  tags                = var.tags
}

resource "azurerm_linux_web_app" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.this.id
  https_only          = var.https_only

  identity {
    type = var.identity_type
  }

  site_config {
    ftps_state = "Disabled"
    always_on  = var.always_on

    application_stack {
      dotnet_version = var.dotnet_version
    }
  }

  app_settings = var.app_insights_connection_string != null && var.app_insights_connection_string != ""
    ? merge({
        "APPINSIGHTS_CONNECTION_STRING" = var.app_insights_connection_string,
        "WEBSITE_RUN_FROM_PACKAGE"      = "1",
      }, var.app_settings)
    : merge({
        "WEBSITE_RUN_FROM_PACKAGE" = "1",
      }, var.app_settings)

  dynamic "connection_string" {
    for_each = var.connection_strings
    content {
      name  = connection_string.key
      type  = connection_string.value.type
      value = connection_string.value.value
    }
  }

  tags = var.tags
}

resource "azurerm_monitor_diagnostic_setting" "this" {
  count = var.log_analytics_workspace_id != null && var.log_analytics_workspace_id != "" ? 1 : 0

  name                       = "${var.name}-diag"
  target_resource_id         = azurerm_linux_web_app.this.id
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
