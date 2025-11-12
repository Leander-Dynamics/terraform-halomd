// Diagnostics to Storage (v4-compliant)
// These settings are optional and guarded by `enable_diagnostics` and a non-empty
// `diagnostics_storage_account_id`. Categories default to groups to minimize
// maintenance across provider updates.

locals {
  diagnostics_storage_account_id = var.diagnostics_storage_account_id != "" ? var.diagnostics_storage_account_id : module.docs_storage_account.id
  enable_diag                    = var.enable_diagnostics && local.diagnostics_storage_account_id != ""
}

resource "azurerm_monitor_diagnostic_setting" "backend_app" {
  count              = local.enable_diag ? 1 : 0
  name               = format("ds-%s-backend", var.env_region)
  target_resource_id = azurerm_linux_web_app.backend.id
  storage_account_id = local.diagnostics_storage_account_id

  dynamic "enabled_log" {
    for_each = var.diagnostics_log_category_groups
    content {
      category_group = enabled_log.value
    }
  }

  dynamic "enabled_log" {
    for_each = var.diagnostics_log_categories
    content {
      category = enabled_log.value
    }
  }

  dynamic "metric" {
    for_each = var.diagnostics_metric_categories
    content {
      category = metric.value
    }
  }
}

resource "azurerm_monitor_diagnostic_setting" "frontend_app" {
  count              = local.enable_diag ? 1 : 0
  name               = format("ds-%s-frontend", var.env_region)
  target_resource_id = azurerm_linux_web_app.frontend.id
  storage_account_id = local.diagnostics_storage_account_id

  dynamic "enabled_log" {
    for_each = var.diagnostics_log_category_groups
    content {
      category_group = enabled_log.value
    }
  }

  dynamic "enabled_log" {
    for_each = var.diagnostics_log_categories
    content {
      category = enabled_log.value
    }
  }

  dynamic "metric" {
    for_each = var.diagnostics_metric_categories
    content {
      category = metric.value
    }
  }
}
