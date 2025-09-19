resource "azurerm_service_plan" "cron" {
  name                = local.names.cron_plan
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  os_type             = "Linux"
  sku_name            = "EP1"
  tags                = var.tags
}

resource "azurerm_application_insights" "cron" {
  name                = local.names.cron_insights
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_linux_function_app" "cron" {
  name                       = local.names.cron_app
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  service_plan_id            = azurerm_service_plan.cron.id
  storage_account_name       = module.cron_storage_account.name
  storage_account_access_key = module.cron_storage_account.primary_access_key
  virtual_network_subnet_id  = local.subnet_ids.app_services
  public_network_access_enabled = false
  tags                       = var.tags

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY        = azurerm_application_insights.cron.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.cron.connection_string
    AzureWebJobsStorage                   = module.cron_storage_account.primary_connection_string
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
      tags["hidden-link: /app-insights-resource-id"],
    ]
  }
}

resource "azurerm_private_endpoint" "cron" {
  name                = local.names.cron_endpoint
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id           = local.subnet_ids.services
  tags                = var.tags

  private_service_connection {
    name                           = format("%s-cron-psc", var.env_region)
    private_connection_resource_id = azurerm_linux_function_app.cron.id
    subresource_names              = ["sites"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_a_record" "cron" {
  name                = azurerm_linux_function_app.cron.name
  zone_name           = data.azurerm_private_dns_zone.function_dns.name
  resource_group_name = data.azurerm_private_dns_zone.function_dns.resource_group_name
  ttl                 = 300
  records             = [azurerm_private_endpoint.cron.private_service_connection[0].private_ip_address]
  provider            = azurerm.hub
}

resource "azurerm_service_plan" "external" {
  count = var.enable_external_api ? 1 : 0
  name                = local.names.external_plan
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  os_type             = "Linux"
  sku_name            = "EP1"
  tags                = var.tags
}

resource "azurerm_application_insights" "external" {
  count = var.enable_external_api ? 1 : 0
  name                = local.names.external_insights
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_network_security_group" "external_function" {
  name                = local.names.external_nsg
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  tags                = var.tags
}

resource "azurerm_network_security_rule" "external_https" {
  name                        = "Allow-HTTPS-Inbound"
  priority                    = 100
  direction                   = "Inbound"
  access                      = "Allow"
  protocol                    = "Tcp"
  source_port_range           = "*"
  destination_port_range      = "443"
  source_address_prefix       = "*"
  destination_address_prefix  = "*"
  resource_group_name         = module.resource_group.name
  network_security_group_name = azurerm_network_security_group.external_function.name
}

resource "azurerm_subnet_network_security_group_association" "external" {
  subnet_id                 = local.subnet_ids.app_services
  network_security_group_id = azurerm_network_security_group.external_function.id
}

resource "azurerm_linux_function_app" "external" {
  count = var.enable_external_api ? 1 : 0
  name                       = local.names.external_app
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  service_plan_id            = azurerm_service_plan.external.id
  storage_account_name       = module.external_storage_account.name
  storage_account_access_key = module.external_storage_account.primary_access_key
  virtual_network_subnet_id  = local.subnet_ids.app_services
  public_network_access_enabled = false
  tags                       = var.tags

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY        = azurerm_application_insights.external.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.external.connection_string
    FUNCTIONS_WORKER_RUNTIME              = "node"
    AzureWebJobsStorage                   = module.external_storage_account.primary_connection_string
    AzureWebJobsDashboard                 = module.external_storage_account.primary_connection_string
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
      tags["hidden-link: /app-insights-resource-id"],
    ]
  }
}
