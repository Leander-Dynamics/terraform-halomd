resource "azurerm_service_plan" "plan" {
  name                = var.plan_name
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.plan_sku
  tags                = var.tags
}

resource "azurerm_linux_web_app" "app" {
  name                = var.app_name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = var.https_only

  identity { type = "SystemAssigned" }

  site_config {
    always_on = var.always_on
    ftps_state = var.ftps_state

    application_stack {
      dotnet_version = var.runtime_stack == "dotnet" ? var.runtime_version : null
      node_version   = var.runtime_stack == "node"   ? var.runtime_version : null
      python_version = var.runtime_stack == "python" ? var.runtime_version : null
    }
  }

  app_settings = var.app_settings

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

resource "azurerm_monitor_diagnostic_setting" "app" {
  count = trimspace(coalesce(var.log_analytics_workspace_id, "")) != "" ? 1 : 0

  name                       = "${var.app_name}-diag"
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
