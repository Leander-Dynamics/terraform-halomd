locals {
  normalized_runtime_stack = lower(trimspace(coalesce(var.runtime_stack, "dotnet")))

  arbitration_plan_name = format(
    "asp-%s-arb-%s-%s",
    trimspace(var.project_name),
    trimspace(var.env_name),
    trimspace(var.location),
  )

  arbitration_app_name = format(
    "app-%s-arb-%s",
    trimspace(var.project_name),
    trimspace(var.env_name),
  )

  arbitration_plan_sku = try(trimspace(var.arbitration_app_plan_sku), "") != ""
    ? var.arbitration_app_plan_sku
    : var.plan_sku

  arbitration_app_insights_connection_string = try(trimspace(var.arbitration_app_insights_connection_string), "") != ""
    ? var.arbitration_app_insights_connection_string
    : var.app_insights_connection_string

  arbitration_log_analytics_workspace_id = try(trimspace(var.arbitration_log_analytics_workspace_id), "") != ""
    ? var.arbitration_log_analytics_workspace_id
    : var.log_analytics_workspace_id
}

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
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on = var.always_on
    ftps_state = "Disabled"

    application_stack {
      dotnet_version = local.normalized_runtime_stack == "dotnet" ? var.runtime_version : null
      node_version   = local.normalized_runtime_stack == "node"   ? var.runtime_version : null
      python_version = local.normalized_runtime_stack == "python" ? var.runtime_version : null
    }
  }

  app_settings = merge(
    {
      "APPINSIGHTS_CONNECTION_STRING" = var.app_insights_connection_string
      "APPINSIGHTS_CONNECTIONSTRING"  = var.app_insights_connection_string
    },
    var.run_from_package == true ? { "WEBSITE_RUN_FROM_PACKAGE" = "1" } : {},
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

# -----------------------------------------------------------------------------
# Optional arbitration App Service (controlled via enable_arbitration_app_service)
# -----------------------------------------------------------------------------
module "arbitration_app_service" {
  count  = var.enable_arbitration_app_service ? 1 : 0
  source = "../../Azure/modules/web-app"

  name                = local.arbitration_app_name
  app_name            = local.arbitration_app_name
  plan_name           = local.arbitration_plan_name
  plan_sku            = local.arbitration_plan_sku
  resource_group_name = var.resource_group_name
  location            = var.location

  runtime_stack   = var.arbitration_runtime_stack
  runtime_version = var.arbitration_runtime_version

  app_insights_connection_string = local.arbitration_app_insights_connection_string
  log_analytics_workspace_id     = local.arbitration_log_analytics_workspace_id
  run_from_package               = var.arbitration_run_from_package
  app_settings                   = var.arbitration_app_settings
  connection_strings             = var.arbitration_connection_strings
  tags                           = var.tags
}

output "arbitration_app_service_name" {
  description = "Name of the arbitration App Service when enabled."
  value       = try(module.arbitration_app_service[0].name, null)
}

output "arbitration_app_service_default_hostname" {
  description = "Default hostname assigned to the arbitration App Service when enabled."
  value       = try(module.arbitration_app_service[0].default_hostname, null)
}
