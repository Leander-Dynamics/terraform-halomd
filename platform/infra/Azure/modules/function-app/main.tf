resource "random_string" "sa" {
  length  = 6
  upper   = false
  lower   = true
  numeric = true
  special = false
}
resource "azurerm_storage_account" "sa" {
  name                     = replace("${var.name}${random_string.sa.result}", "-", "")
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}
resource "azurerm_linux_function_app" "func" {
  name                        = var.name
  resource_group_name         = var.resource_group_name
  location                    = var.location
  service_plan_id             = var.service_plan_id
  storage_account_name        = azurerm_storage_account.sa.name
  storage_account_access_key  = azurerm_storage_account.sa.primary_access_key
  https_only                  = true
  functions_extension_version = "~4"
  identity { type = "SystemAssigned" }
  site_config {
    ftps_state = "Disabled"
    application_stack {
      dotnet_version = var.runtime == "dotnet" ? "8.0"  : null
      python_version = var.runtime == "python" ? "3.10" : null
      node_version   = var.runtime == "node"   ? "~18"  : null
    }
  }
  app_settings = merge({
    "FUNCTIONS_WORKER_RUNTIME"      = var.runtime
    "APPINSIGHTS_CONNECTION_STRING" = var.app_insights_connection_string
    "WEBSITE_RUN_FROM_PACKAGE"      = "1"
  }, var.app_settings)
  tags = var.tags
}
