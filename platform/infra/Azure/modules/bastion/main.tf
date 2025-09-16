locals {
  public_ip_name              = coalesce(var.public_ip_name, "${var.name}-pip")
  effective_scale_units       = var.sku == "Standard" ? var.scale_units : null
  effective_file_copy_enabled = var.sku == "Standard" ? var.file_copy_enabled : false
  effective_ip_connect        = var.sku == "Standard" ? var.ip_connect_enabled : false
  effective_shareable_link    = var.sku == "Standard" ? var.shareable_link_enabled : false
  effective_tunneling         = var.sku == "Standard" ? var.tunneling_enabled : false
}

resource "azurerm_public_ip" "this" {
  name                = local.public_ip_name
  location            = var.location
  resource_group_name = var.resource_group_name
  allocation_method   = var.public_ip_allocation_method
  sku                 = var.public_ip_sku
  zones               = var.zones
  tags                = var.tags
}

resource "azurerm_bastion_host" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku
  scale_units         = local.effective_scale_units

  copy_paste_enabled     = var.copy_paste_enabled
  file_copy_enabled      = local.effective_file_copy_enabled
  ip_connect_enabled     = local.effective_ip_connect
  shareable_link_enabled = local.effective_shareable_link
  tunneling_enabled      = local.effective_tunneling

  tags  = var.tags
  zones = var.zones

  ip_configuration {
    name                 = var.ip_configuration_name
    subnet_id            = var.subnet_id
    public_ip_address_id = azurerm_public_ip.this.id
  }
}
