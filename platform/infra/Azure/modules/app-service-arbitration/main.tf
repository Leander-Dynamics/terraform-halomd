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
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = true

  identity { type = "SystemAssigned" }

  site_config {
    always_on = true
    ftps_state = "Disabled"

    application_stack {
      dotnet_version = var.runtime_stack == "dotnet" ? var.runtime_version : null
      node_version   = var.runtime_stack == "node"   ? var.runtime_version : null
      python_version = var.runtime_stack == "python" ? var.runtime_version : null
    }
  }

  app_settings = merge(
    {
      "APPINSIGHTS_CONNECTION_STRING" = var.app_insights_connection_string
      "APPINSIGHTS_CONNECTIONSTRING"  = var.app_insights_connection_string
    },
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

  tags = var.tags
}
