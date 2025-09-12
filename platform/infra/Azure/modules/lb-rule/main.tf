resource "azurerm_lb_rule" "this" {
  name                           = var.name
  resource_group_name            = var.resource_group_name
  loadbalancer_id                = var.loadbalancer_id
  protocol                       = var.protocol
  frontend_port                  = var.frontend_port
  backend_port                   = var.backend_port
  frontend_ip_configuration_name = var.frontend_ip_name
  backend_address_pool_id        = var.backend_pool_id
  probe_id                       = var.probe_id
}
