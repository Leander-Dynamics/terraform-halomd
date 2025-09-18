# this is for for the external function
resource "azurerm_service_plan" "arbit-workflow-application-external-function-app-serviceplan-1" {
  name                = "${var.env_region}-arbit-workflow-application-ext-functions-asp"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  os_type             = "Linux"
  sku_name            = "EP1"
}

# External Function App Insights
resource "azurerm_application_insights" "external_functions_ai" {
  name                = "${var.env_region}-arbit-workflow-application-external-functions-ai"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  application_type    = "web"
}


resource "azurerm_network_security_group" "workflow_external_function_nsg" {
  name                = "${var.env_region}-workflow-external-function-nsg"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
}

# allow inbound 443 (for Graph calls)
resource "azurerm_network_security_rule" "allow_https_inbound" {
  name                        = "Allow-HTTPS-Inbound"
  priority                    = 100
  direction                   = "Inbound"
  access                      = "Allow"
  protocol                    = "Tcp"
  source_port_range           = "*"
  destination_port_range      = "443"
  source_address_prefix       = "*"
  destination_address_prefix  = "*"
  resource_group_name         = azurerm_resource_group.workflow_rg.name
  network_security_group_name = azurerm_network_security_group.workflow_external_function_nsg.name
}


# Associate NSG with the private_asps_subnet
resource "azurerm_subnet_network_security_group_association" "workflow_external_function_nsg_assoc" {
  subnet_id                 = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-asps-snet-1"
  network_security_group_id = azurerm_network_security_group.workflow_external_function_nsg.id
}

resource "azurerm_linux_function_app" "arbit-workflow-application-ext-function-app" {
  name                = "${var.env_region}-arbit-workflow-application-ext-function-app"
  resource_group_name = azurerm_resource_group.workflow_rg.name
  location            = azurerm_resource_group.workflow_rg.location
  service_plan_id = azurerm_service_plan.arbit-workflow-application-external-function-app-serviceplan-1.id

  # this one should be deveus2workflowextsa meaning it is used by that function
  storage_account_name       = azurerm_storage_account.workflow_storage_account_external_function.name
  storage_account_access_key = azurerm_storage_account.workflow_storage_account_external_function.primary_access_key
  virtual_network_subnet_id  = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-asps-snet-1"

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.external_functions_ai.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.external_functions_ai.connection_string
    "FUNCTIONS_WORKER_RUNTIME"              = "node"
    "AzureWebJobsStorage"                   = azurerm_storage_account.workflow_storage_account_external_function.primary_connection_string
    "AzureWebJobsDashboard"                 = azurerm_storage_account.workflow_storage_account_external_function.primary_connection_string

  }

  site_config {
    application_stack {
      node_version = "22"

    }
    vnet_route_all_enabled = true
  }

  lifecycle {
    ignore_changes = [
      app_settings["AzureWebJobsStorage"],
      sticky_settings,
      site_config,
      app_settings,
      tags["hidden-link: /app-insights-resource-id"]
    ]
  }
}
