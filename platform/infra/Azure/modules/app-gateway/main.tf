resource "azurerm_public_ip" "this" {
  name                = "${var.name}-pip"
  location            = var.location
  resource_group_name = var.resource_group_name
  allocation_method   = "Static"
  sku                 = "Standard"
  domain_name_label   = var.fqdn_prefix
  tags                = var.tags
}

resource "azurerm_application_gateway" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  tags                = var.tags
  enable_http2        = var.enable_http2

  sku {
    name     = var.sku_name
    tier     = var.sku_tier
    capacity = var.sku_capacity
  }

  gateway_ip_configuration {
    name      = "gateway-ip-config"
    subnet_id = var.subnet_id
  }

  frontend_port {
    name = "frontend-port"
    port = var.frontend_port
  }

  frontend_ip_configuration {
    name                 = "public-frontend"
    public_ip_address_id = azurerm_public_ip.this.id
  }

  backend_address_pool {
    name  = "default-backend"
    fqdns = var.backend_fqdns
  }

  backend_http_settings {
    name                  = "http-settings"
    cookie_based_affinity = "Disabled"
    port                  = var.backend_port
    protocol              = var.backend_protocol
    request_timeout       = var.backend_request_timeout
    pick_host_name_from_backend_address = var.pick_host_name_from_backend_address
  }

  http_listener {
    name                           = "listener-http"
    frontend_ip_configuration_name = "public-frontend"
    frontend_port_name             = "frontend-port"
    protocol                       = var.listener_protocol
  }

  request_routing_rule {
    name                       = "routing-rule"
    rule_type                  = "Basic"
    http_listener_name         = "listener-http"
    backend_address_pool_name  = "default-backend"
    backend_http_settings_name = "http-settings"
    priority                   = 100
  }

  dynamic "ssl_certificate" {
    for_each = var.ssl_certificate == null ? [] : [var.ssl_certificate]
    content {
      name     = ssl_certificate.value.name
      data     = ssl_certificate.value.data
      password = ssl_certificate.value.password
    }
  }

  dynamic "trusted_client_certificate" {
    for_each = var.trusted_client_certificates
    content {
      name = trusted_client_certificate.value.name
      data = trusted_client_certificate.value.data
    }
  }

  lifecycle {
    ignore_changes = [backend_address_pool]
  }
}
