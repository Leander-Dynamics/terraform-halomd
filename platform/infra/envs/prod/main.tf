locals {
  resource_group_name = try(trimspace(var.resource_group_name), "") != "" ? var.resource_group_name : "rg-${var.project_name}-${var.env_name}"
  app_service_plan_name = try(trimspace(var.app_service_plan_name), "") != "" ? var.app_service_plan_name : "asp-${var.project_name}-web-${var.env_name}-${var.location}"
  app_service_name =
    try(trimspace(var.app_service_name), "") != "" ? var.app_service_name :
    try(trimspace(var.app_service_fqdn_prefix), "") != "" ? var.app_service_fqdn_prefix :
    "app-${var.project_name}-web-${var.env_name}"
}

resource "azurerm_service_plan" "plan" {
  name                = local.app_service_plan_name
  location            = var.location
  resource_group_name = local.resource_group_name
  os_type             = "Linux"
  sku_name            = var.app_service_plan_sku
  tags                = var.tags
}

resource "azurerm_linux_web_app" "app" {
  name                = local.app_service_name
  location            = var.location
  resource_group_name = local.resource_group_name
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on  = true
    ftps_state = "Disabled"

    application_stack {
      dotnet_version = var.app_service_dotnet_version
    }
  }

  app_settings = merge(
    {
      "APPINSIGHTS_CONNECTION_STRING" = var.app_service_app_insights_connection_string
      "APPINSIGHTS_CONNECTIONSTRING"  = var.app_service_app_insights_connection_string
    },
    var.app_service_run_from_package ? { "WEBSITE_RUN_FROM_PACKAGE" = "1" } : {},
    var.app_service_app_settings
  )

  dynamic "connection_string" {
    for_each = var.app_service_connection_strings
    content {
      name  = connection_string.key
      type  = connection_string.value.type
      value = connection_string.value.value
    }
  }

  tags = var.tags
}

resource "azurerm_monitor_diagnostic_setting" "app" {
  count = try(trimspace(var.app_service_log_analytics_workspace_id), "") != "" ? 1 : 0

  name                       = "${local.app_service_name}-diag"
  target_resource_id         = azurerm_linux_web_app.app.id
  log_analytics_workspace_id = var.app_service_log_analytics_workspace_id

  dynamic "log" {
    for_each = [
      "AppServiceHTTPLogs",
      "AppServiceConsoleLogs",
      "AppServiceAppLogs",
      "AppServiceAuditLogs",
      "AppServiceFileAuditLogs",
      "AppServicePlatformLogs"
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
