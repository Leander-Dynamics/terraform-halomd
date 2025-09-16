locals {
  app_insights_connection_string_trimmed = trimspace(coalesce(var.app_insights_connection_string, ""))
  log_analytics_workspace_id_trimmed      = trimspace(coalesce(var.log_analytics_workspace_id, ""))
  enable_diagnostics                      = local.log_analytics_workspace_id_trimmed != ""
}

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
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.this.id
  https_only          = var.https_only
  tags                = var.tags

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on  = var.always_on
    ftps_state = var.ftps_state

    application_stack {
      dotnet_version = var.dotnet_version
    }
  }

  app_settings = merge(
    local.app_insights_connection_string_trimmed != "" ? {
      "APPINSIGHTS_CONNECTION_STRING" = local.app_insights_connection_string_trimmed
      "APPINSIGHTS_CONNECTIONSTRING"  = local.app_insights_connection_string_trimmed
    } : {},
    var.run_from_package ? { "WEBSITE_RUN_FROM_PACKAGE" = "1" } : {},
    var.app_settings
  )

  dynamic "connection_string" {
    for_each = var.connection_strings
    content {
      name  = connection_string.key
      type  = connection_string.value.type
      value = connection_string.value.value
    }
  }
}

resource "azurerm_monitor_diagnostic_setting" "this" {
  count = local.enable_diagnostics ? 1 : 0

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
