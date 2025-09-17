resource "azurerm_lb" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku
  frontend_ip_configuration {
    name                 = "PublicIP"
    public_ip_address_id = var.public_ip_id
  }
}
