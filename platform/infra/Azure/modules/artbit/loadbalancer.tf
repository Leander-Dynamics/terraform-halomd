resource "azurerm_public_ip" "workflow" {
  name                = local.names.load_balancer_pip
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  allocation_method   = "Static"
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_lb" "workflow" {
  name                = local.names.load_balancer
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  sku                 = "Standard"
  tags                = var.tags

  frontend_ip_configuration {
    name                 = "PublicIPAddress"
    public_ip_address_id = azurerm_public_ip.workflow.id
  }
}
