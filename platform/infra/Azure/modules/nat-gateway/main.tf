locals {
  public_ip_configurations = [
    for cfg in var.public_ip_configurations : {
      name              = cfg.name
      allocation_method = try(cfg.allocation_method, "Static")
      sku               = try(cfg.sku, "Standard")
      zones             = try(cfg.zones, [])
      tags              = try(cfg.tags, {})
    }
  ]
}

resource "azurerm_public_ip" "this" {
  for_each            = { for cfg in local.public_ip_configurations : cfg.name => cfg }
  name                = each.value.name
  location            = var.location
  resource_group_name = var.resource_group_name
  allocation_method   = each.value.allocation_method
  sku                 = each.value.sku
  zones               = length(each.value.zones) > 0 ? each.value.zones : null
  tags                = merge(var.tags, each.value.tags)
}

resource "azurerm_nat_gateway" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku_name            = var.sku_name
  idle_timeout_in_minutes = var.idle_timeout_in_minutes
  zones                   = length(var.zones) > 0 ? var.zones : null
  tags                    = var.tags
}

locals {
  created_public_ip_ids = [for pip in azurerm_public_ip.this : pip.id]
  effective_public_ip_ids = distinct(concat(var.public_ip_ids, local.created_public_ip_ids))
}

resource "azurerm_nat_gateway_public_ip_association" "this" {
  for_each = toset(local.effective_public_ip_ids)

  nat_gateway_id      = azurerm_nat_gateway.this.id
  public_ip_address_id = each.value
}

resource "azurerm_subnet_nat_gateway_association" "this" {
  for_each = toset(var.subnet_ids)

  subnet_id      = each.value
  nat_gateway_id = azurerm_nat_gateway.this.id
}
