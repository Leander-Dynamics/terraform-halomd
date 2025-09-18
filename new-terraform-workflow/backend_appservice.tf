# this is for backend application service plan
resource "azurerm_service_plan" "arbit_workflow_application_backend_app_serviceplan_1" {
  name                = "${var.env_region}-arbit-workflow-application-backend-asp-1"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  os_type             = "Linux"
  sku_name            = "P1v3"
}

# Backend App Insights
resource "azurerm_application_insights" "backend_ai" {
  name                = "${var.env_region}-arbit-workflow-application-backend-ai"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  application_type    = "web"
}

# the backend app
resource "azurerm_linux_web_app" "arbit_workflow_application_backend" {
  name                                           = "${var.env_region}-arbit-workflow-application-backend"
  location                                       = azurerm_resource_group.workflow_rg.location
  resource_group_name                            = azurerm_resource_group.workflow_rg.name
  service_plan_id                                = azurerm_service_plan.arbit_workflow_application_backend_app_serviceplan_1.id
  public_network_access_enabled                  = false
  virtual_network_subnet_id                      = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-asps-snet-1"
  webdeploy_publish_basic_authentication_enabled = false
  ftp_publish_basic_authentication_enabled       = false

  site_config {
    minimum_tls_version = "1.2"
    application_stack {
      node_version = "20-lts"
    }
  }

  app_settings = {
    # # comment these out for now
    # "WEBSITE_RUN_FROM_PACKAGE" = "1" # used if deploying ZIP
    # "STORAGE_CONTAINER_NAME"   = azurerm_storage_container.somecontainer.name
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.backend_ai.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.backend_ai.connection_string
  }

  lifecycle {
    ignore_changes = [
      app_settings,
      site_config,
      tags["hidden-link: /app-insights-resource-id"]
    ]
  }
}

resource "azurerm_private_endpoint" "workflow_backend_private_ep" {
  name                = "${var.env_region}-arbit-workflow-application-backend-private-ep-1"
  location            = azurerm_linux_web_app.arbit_workflow_application_backend.location
  resource_group_name = azurerm_linux_web_app.arbit_workflow_application_backend.resource_group_name
  subnet_id           = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-services-snet-1"
  private_service_connection {
    name                           = "${var.env_region}-backend-psc"
    private_connection_resource_id = azurerm_linux_web_app.arbit_workflow_application_backend.id
    subresource_names = ["sites"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_a_record" "backend_record" {
  name                = azurerm_linux_web_app.arbit_workflow_application_backend.name
  zone_name           = data.azurerm_private_dns_zone.function_dns.name
  resource_group_name = data.azurerm_private_dns_zone.function_dns.resource_group_name
  ttl                 = 300
  records = [azurerm_private_endpoint.workflow_backend_private_ep.private_service_connection.0.private_ip_address]
  provider            = azurerm.hub
}
