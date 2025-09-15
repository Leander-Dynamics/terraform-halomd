locals {
  sanitized_storage_account_input = regexreplace(lower(trimspace(var.storage_account_name)), "[^0-9a-z]", "")
  storage_account_name_provided   = local.sanitized_storage_account_input != ""
  storage_account_seed            = local.storage_account_name_provided ? local.sanitized_storage_account_input : regexreplace(lower(var.name), "[^0-9a-z]", "")
  storage_account_fallback_seed   = local.storage_account_seed != "" ? local.storage_account_seed : "funcapp"
  storage_account_base            = substr(local.storage_account_name_provided ? local.sanitized_storage_account_input : local.storage_account_fallback_seed, 0, 18)
}

resource "random_string" "storage_account_suffix" {
  count   = local.storage_account_name_provided ? 0 : 1
  length  = 6
  upper   = false
  lower   = true
  numeric = true
  special = false
}

locals {
  storage_account_name                  = local.storage_account_name_provided ? local.sanitized_storage_account_input : substr("${local.storage_account_base}${random_string.storage_account_suffix[0].result}", 0, 24)
  runtime_stack_normalized              = lower(trimspace(var.runtime_stack))
  runtime_version_effective             = trimspace(var.runtime_version) != "" ? trimspace(var.runtime_version) : (
    local.runtime_stack_normalized == "python" ? "3.10" :
    local.runtime_stack_normalized == "node"   ? "~18" :
    "8.0"
  )
  application_insights_connection_string = trimspace(var.application_insights_connection_string)
  log_analytics_workspace_id_trimmed      = trimspace(coalesce(var.log_analytics_workspace_id, ""))
  enable_diagnostics                      = local.log_analytics_workspace_id_trimmed != ""
}

resource "azurerm_storage_account" "this" {
  name                     = local.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = var.storage_account_tier
  account_replication_type = var.storage_account_replication_type
  account_kind             = "StorageV2"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

resource "azurerm_service_plan" "this" {
  name                = var.plan_name
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.plan_sku
  tags                = var.tags
}

resource "azurerm_linux_function_app" "this" {
  name                        = var.name
  resource_group_name         = var.resource_group_name
  location                    = var.location
  service_plan_id             = azurerm_service_plan.this.id
  storage_account_name        = azurerm_storage_account.this.name
  storage_account_access_key  = azurerm_storage_account.this.primary_access_key
  https_only                  = true
  functions_extension_version = var.functions_extension_version
  tags                        = var.tags

  identity {
    type = "SystemAssigned"
  }

  site_config {
    ftps_state = "Disabled"

    application_stack {
      dotnet_version = local.runtime_stack_normalized == "dotnet" ? local.runtime_version_effective : null
      node_version   = local.runtime_stack_normalized == "node"   ? local.runtime_version_effective : null
      python_version = local.runtime_stack_normalized == "python" ? local.runtime_version_effective : null
    }
  }

  app_settings = merge(
    {
      "FUNCTIONS_WORKER_RUNTIME" = local.runtime_stack_normalized
      "WEBSITE_RUN_FROM_PACKAGE" = "1"
    },
    local.application_insights_connection_string != "" ? {
      "APPINSIGHTS_CONNECTION_STRING" = local.application_insights_connection_string
    } : {},
    var.app_settings
  )
}

resource "azurerm_monitor_diagnostic_setting" "this" {
  count = local.enable_diagnostics ? 1 : 0

  name                       = "${var.name}-diag"
  target_resource_id         = azurerm_linux_function_app.this.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  log {
    category = "FunctionAppLogs"
    enabled  = true

    retention_policy {
      enabled = false
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
