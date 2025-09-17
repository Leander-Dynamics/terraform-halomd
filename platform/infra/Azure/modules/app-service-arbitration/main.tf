resource "azurerm_service_plan" "this" {
  name                = var.plan_name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_name            = var.plan_sku
  os_type             = "Windows"
}

resource "azurerm_windows_web_app" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.this.id

  site_config {
    application_stack {
      current_stack  = var.runtime_stack
      dotnet_version = var.runtime_version
    }
    always_on = true
  }

  https_only   = true
  app_settings = var.app_settings

  dynamic "connection_string" {
    for_each = var.connection_strings
    content {
      name  = connection_string.value.name
      type  = connection_string.value.type
      value = connection_string.value.value
    }
  }

  tags = var.tags
}

resource "azurerm_monitor_diagnostic_setting" "this" {
  name                       = "${var.name}-diag"
  target_resource_id         = azurerm_windows_web_app.this.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "AppServiceConsoleLogs"
  }

  enabled_log {
    category = "AppServiceAppLogs"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}
