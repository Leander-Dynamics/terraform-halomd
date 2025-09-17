resource "azurerm_public_ip" "this" {
  name                = "${var.name}-pip"
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

  sku {
    name     = var.sku_name
    tier     = var.sku_tier
    capacity = var.sku_capacity
  }

  gateway_ip_configuration {
    name      = "gwipcfg"
    subnet_id = var.subnet_id
  }

  frontend_ip_configuration {
    name                 = "PublicFrontend"
    public_ip_address_id = azurerm_public_ip.this.id
  }

  frontend_port {
    name = "frontendPort"
    port = var.frontend_port
  }

  backend_address_pool {
    name  = "backendPool"
    fqdns = var.backend_fqdns
  }

  backend_http_settings {
    name                                = "backendHttp"
    port                                = var.backend_port
    protocol                            = var.backend_protocol
    request_timeout                     = var.backend_request_timeout
    cookie_based_affinity               = "Disabled"
    pick_host_name_from_backend_address = var.pick_host_name_from_backend_address
  }

  http_listener {
    name                           = "listener"
    frontend_ip_configuration_name = "PublicFrontend"
    frontend_port_name             = "frontendPort"
    protocol                       = var.listener_protocol
  }

  request_routing_rule {
    name                       = "rule1"
    rule_type                  = "Basic"
    http_listener_name         = "listener"
    backend_address_pool_name  = "backendPool"
    backend_http_settings_name = "backendHttp"
  }

  enable_http2 = var.enable_http2
  tags         = var.tags
}
