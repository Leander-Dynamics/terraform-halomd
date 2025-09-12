resource "azurerm_linux_web_app" "app" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = var.service_plan_id
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
