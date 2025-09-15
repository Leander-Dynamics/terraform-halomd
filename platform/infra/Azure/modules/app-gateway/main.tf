locals {
  gateway_ip_configuration_name   = "app-gateway-ip-config"
  frontend_ip_configuration_name  = "public-frontend"
  frontend_port_name              = "frontend-port"
  backend_address_pool_name       = "default-backend-pool"
  backend_http_settings_name      = "https-settings"
  probe_name                      = "https-probe"
  listener_name                   = "frontend-listener"
  routing_rule_name               = "https-routing-rule"
  public_ip_name                  = "${var.name}-pip"
}

resource "azurerm_public_ip" "this" {
  name                = local.public_ip_name
  location            = var.location
  resource_group_name = var.resource_group_name
  allocation_method   = "Static"
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_application_gateway" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  enable_http2        = var.enable_http2
  tags                = var.tags

  sku {
    name     = var.sku_name
    tier     = var.sku_tier
    capacity = var.sku_capacity
  }

  gateway_ip_configuration {
    name      = local.gateway_ip_configuration_name
    subnet_id = var.subnet_id
  }

  frontend_ip_configuration {
    name                 = local.frontend_ip_configuration_name
    public_ip_address_id = azurerm_public_ip.this.id
  }

  frontend_port {
    name = local.frontend_port_name
    port = var.frontend_port
  }

  backend_address_pool {
    name  = local.backend_address_pool_name
    fqdns = var.backend_fqdns
  }

  probe {
    name                                      = local.probe_name
    protocol                                  = "Https"
    path                                      = var.health_probe_path
    interval                                  = var.health_probe_interval
    timeout                                   = var.health_probe_timeout
    unhealthy_threshold                       = var.health_probe_unhealthy_threshold
    pick_host_name_from_backend_http_settings = true
  }

  backend_http_settings {
    name                                = local.backend_http_settings_name
    protocol                            = "Https"
    port                                = var.backend_port
    cookie_based_affinity               = "Disabled"
    request_timeout                     = var.request_timeout
    probe_name                          = local.probe_name
    pick_host_name_from_backend_address = true
  }

  dynamic "ssl_certificate" {
    for_each = var.frontend_protocol == "Https" && var.ssl_certificate != null ? [var.ssl_certificate] : []
    content {
      name     = ssl_certificate.value.name
      data     = ssl_certificate.value.data
      password = ssl_certificate.value.password
    }
  }

  http_listener {
    name                           = local.listener_name
    frontend_ip_configuration_name = local.frontend_ip_configuration_name
    frontend_port_name             = local.frontend_port_name
    protocol                       = var.frontend_protocol
    ssl_certificate_name           = var.frontend_protocol == "Https" && var.ssl_certificate != null ? var.ssl_certificate.name : null
  }

  request_routing_rule {
    name                       = local.routing_rule_name
    rule_type                  = "Basic"
    http_listener_name         = local.listener_name
    backend_address_pool_name  = local.backend_address_pool_name
    backend_http_settings_name = local.backend_http_settings_name
  }
}
