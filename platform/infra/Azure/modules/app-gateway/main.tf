locals {
  gateway_ip_configuration_name = "gateway-ip-configuration"
  frontend_ip_configuration_name = "public-frontend-ip"
  frontend_port_name             = "frontend-port"
  backend_address_pool_name      = "backend-address-pool"
  backend_http_settings_name     = "backend-http-settings"
  http_listener_name             = "default-listener"
  request_routing_rule_name      = "default-routing-rule"
  probe_name                     = "default-health-probe"
  ssl_certificate_config         = var.ssl_certificate == null ? [] : [var.ssl_certificate]
}

resource "azurerm_public_ip" "this" {
  name                = "${var.name}-pip"
  resource_group_name = var.resource_group_name
  location            = var.location
  allocation_method   = "Static"
  sku                 = "Standard"
  domain_name_label   = var.fqdn_prefix
  tags                = var.tags
}

resource "azurerm_application_gateway" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
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

  frontend_port {
    name = local.frontend_port_name
    port = var.frontend_port
  }

  frontend_ip_configuration {
    name                 = local.frontend_ip_configuration_name
    public_ip_address_id = azurerm_public_ip.this.id
  }

  backend_address_pool {
    name  = local.backend_address_pool_name
    fqdns = var.backend_fqdns
  }

  probe {
    name                                     = local.probe_name
    protocol                                 = var.backend_protocol
    path                                     = var.health_probe_path
    interval                                 = var.health_probe_interval
    timeout                                  = var.health_probe_timeout
    unhealthy_threshold                      = var.health_probe_unhealthy_threshold
    pick_host_name_from_backend_http_settings = var.pick_host_name_from_backend_address
    port                                     = var.backend_port
  }

  backend_http_settings {
    name                                = local.backend_http_settings_name
    cookie_based_affinity               = "Disabled"
    port                                = var.backend_port
    protocol                            = var.backend_protocol
    pick_host_name_from_backend_address = var.pick_host_name_from_backend_address
    request_timeout                     = var.backend_request_timeout
    probe_name                          = local.probe_name
  }

  dynamic "trusted_client_certificate" {
    for_each = var.trusted_client_certificates

    content {
      name = trusted_client_certificate.value.name
      data = trusted_client_certificate.value.data
    }
  }

  dynamic "ssl_certificate" {
    for_each = local.ssl_certificate_config

    content {
      name     = ssl_certificate.value.name
      data     = ssl_certificate.value.data
      password = ssl_certificate.value.password
    }
  }

  http_listener {
    name                           = local.http_listener_name
    frontend_ip_configuration_name = local.frontend_ip_configuration_name
    frontend_port_name             = local.frontend_port_name
    protocol                       = var.listener_protocol
    ssl_certificate_name           = var.listener_protocol == "Https" ? try(var.ssl_certificate.name, null) : null
  }

  request_routing_rule {
    name                       = local.request_routing_rule_name
    rule_type                  = "Basic"
    priority                   = 1
    http_listener_name         = local.http_listener_name
    backend_address_pool_name  = local.backend_address_pool_name
    backend_http_settings_name = local.backend_http_settings_name
  }
}
