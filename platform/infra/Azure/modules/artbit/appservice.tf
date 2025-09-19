resource "azurerm_service_plan" "backend" {
  name                = local.names.backend_plan
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  os_type             = "Linux"
  sku_name            = "P1v3"
  tags                = var.tags
}

resource "azurerm_application_insights" "backend" {
  name                = local.names.backend_insights
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_linux_web_app" "backend" {
  name                                           = local.names.backend_app
  location                                       = module.resource_group.location
  resource_group_name                            = module.resource_group.name
  service_plan_id                                = azurerm_service_plan.backend.id
  public_network_access_enabled                  = false
  virtual_network_subnet_id                      = local.subnet_ids.app_services
  webdeploy_publish_basic_authentication_enabled = false
  ftp_publish_basic_authentication_enabled       = false
  https_only                                     = true
  tags                                           = var.tags

  site_config {
    minimum_tls_version = "1.2"

    application_stack {
        dotnet_version = "6.0"
    }
  }

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY        = azurerm_application_insights.backend.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.backend.connection_string
  }

  lifecycle {
    ignore_changes = [
      app_settings,
      site_config,
      tags["hidden-link: /app-insights-resource-id"],
    ]
  }
}

resource "azurerm_private_endpoint" "backend" {
  name                = local.names.backend_endpoint
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id           = local.subnet_ids.services
  tags                = var.tags

  private_service_connection {
    name                           = format("%s-backend-psc", var.env_region)
    private_connection_resource_id = azurerm_linux_web_app.backend.id
    subresource_names              = ["sites"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_a_record" "backend" {
  name                = azurerm_linux_web_app.backend.name
  zone_name           = data.azurerm_private_dns_zone.function_dns.name
  resource_group_name = data.azurerm_private_dns_zone.function_dns.resource_group_name
  ttl                 = 300
  records             = [azurerm_private_endpoint.backend.private_service_connection[0].private_ip_address]
  provider            = azurerm.hub
}

resource "azurerm_service_plan" "frontend" {
  name                = local.names.frontend_plan
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  os_type             = "Linux"
  sku_name            = "P1v3"
  tags                = var.tags
}

resource "azurerm_application_insights" "frontend" {
  name                = local.names.frontend_insights
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_linux_web_app" "frontend" {
  name                                           = local.names.frontend_app
  location                                       = module.resource_group.location
  resource_group_name                            = module.resource_group.name
  service_plan_id                                = azurerm_service_plan.frontend.id
  public_network_access_enabled                  = false
  virtual_network_subnet_id                      = local.subnet_ids.app_services
  webdeploy_publish_basic_authentication_enabled = false
  ftp_publish_basic_authentication_enabled       = false
  https_only                                     = true
  tags                                           = var.tags

  site_config {
    minimum_tls_version = "1.2"

    application_stack {
        dotnet_version = "6.0"
    }
  }

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY        = azurerm_application_insights.frontend.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.frontend.connection_string
  }

  lifecycle {
    ignore_changes = [
      app_settings,
      site_config,
      tags["hidden-link: /app-insights-resource-id"],
    ]
  }
}

resource "azurerm_private_endpoint" "frontend" {
  name                = local.names.frontend_endpoint
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id           = local.subnet_ids.services
  tags                = var.tags

  private_service_connection {
    name                           = format("%s-frontend-psc", var.env_region)
    private_connection_resource_id = azurerm_linux_web_app.frontend.id
    subresource_names              = ["sites"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_a_record" "frontend" {
  name                = azurerm_linux_web_app.frontend.name
  zone_name           = data.azurerm_private_dns_zone.function_dns.name
  resource_group_name = data.azurerm_private_dns_zone.function_dns.resource_group_name
  ttl                 = 300
  records             = [azurerm_private_endpoint.frontend.private_service_connection[0].private_ip_address]
  provider            = azurerm.hub
}