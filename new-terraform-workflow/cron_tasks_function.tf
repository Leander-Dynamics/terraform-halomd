# this is for for the cron functions
resource "azurerm_service_plan" "arbit-workflow-application-cron-functions-app-serviceplan-1" {
  name                = "${var.env_region}-arbit-workflow-application-cron-functions-asp-1"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  os_type             = "Linux"
  sku_name            = "EP1"
}

# Cron Function App Insights
resource "azurerm_application_insights" "cron_functions_ai" {
  name                = "${var.env_region}-arbit-workflow-application-cron-functions-ai"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  application_type    = "web"
}

# # and then the function app for cron tasks
resource "azurerm_linux_function_app" "arbit-workflow-application-cron-functions-app-1" {
  name                = "${var.env_region}-arbit-workflow-cron-function-app"
  resource_group_name = azurerm_resource_group.workflow_rg.name
  location            = azurerm_resource_group.workflow_rg.location
  service_plan_id = azurerm_service_plan.arbit-workflow-application-cron-functions-app-serviceplan-1.id

  # this one should be deveus2workflowfuncsa meaning it is used by that function
  storage_account_name       = azurerm_storage_account.workflow_storage_account_cron_function.name
  storage_account_access_key = azurerm_storage_account.workflow_storage_account_cron_function.primary_access_key
  virtual_network_subnet_id  = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-asps-snet-1"

  public_network_access_enabled = false

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.cron_functions_ai.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.cron_functions_ai.connection_string
    # "FUNCTIONS_WORKER_RUNTIME" = "node"
    "AzureWebJobsStorage"      = azurerm_storage_account.workflow_storage_account_cron_function.primary_connection_string
  }

  site_config {
    application_stack {
      node_version = "22"
    }
    vnet_route_all_enabled = true
  }
  lifecycle {
    ignore_changes = [
      app_settings["APPINSIGHTS_INSTRUMENTATIONKEY"],
      app_settings["APPLICATIONINSIGHTS_CONNECTION_STRING"],
      app_settings["AzureWebJobsStorage"],
      app_settings["FUNCTIONS_WORKER_RUNTIME"],
      site_config,
      tags["hidden-link: /app-insights-resource-id"]

    ]
  }
}


# private endpoint for function app for crons
resource "azurerm_private_endpoint" "workflow_function_cron_private_ep" {
  name                = "${var.env_region}-arbit-workflow-application-function-crons-ep-1"
  location            = azurerm_linux_function_app.arbit-workflow-application-cron-functions-app-1.location
  resource_group_name = azurerm_linux_function_app.arbit-workflow-application-cron-functions-app-1.resource_group_name
  subnet_id           = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-services-snet-1"

  private_service_connection {
    name                           = "func-psc"
    private_connection_resource_id = azurerm_linux_function_app.arbit-workflow-application-cron-functions-app-1.id
    subresource_names = ["sites"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_a_record" "function_record" {
  name                = azurerm_linux_function_app.arbit-workflow-application-cron-functions-app-1.name
  zone_name           = data.azurerm_private_dns_zone.function_dns.name
  resource_group_name = data.azurerm_private_dns_zone.function_dns.resource_group_name
  ttl                 = 300
  records = [azurerm_private_endpoint.workflow_function_cron_private_ep.private_service_connection.0.private_ip_address]
  provider            = azurerm.hub
}
