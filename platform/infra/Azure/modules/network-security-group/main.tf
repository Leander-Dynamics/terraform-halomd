resource "azurerm_network_security_group" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
}

resource "azurerm_network_security_rule" "this" {
  for_each                    = var.security_rules
  name                        = each.key
  priority                    = each.value.priority
  direction                   = each.value.direction
  access                      = each.value.access
  protocol                    = each.value.protocol
  resource_group_name         = var.resource_group_name
  network_security_group_name = azurerm_network_security_group.this.name

  description                  = try(each.value.description, null)
  source_port_range            = try(each.value.source_port_range, null)
  source_port_ranges           = try(each.value.source_port_ranges, null)
  destination_port_range       = try(each.value.destination_port_range, null)
  destination_port_ranges      = try(each.value.destination_port_ranges, null)
  source_address_prefix        = try(each.value.source_address_prefix, null)
  source_address_prefixes      = try(each.value.source_address_prefixes, null)
  destination_address_prefix   = try(each.value.destination_address_prefix, null)
  destination_address_prefixes = try(each.value.destination_address_prefixes, null)
}

resource "azurerm_subnet_network_security_group_association" "this" {
  for_each = var.subnet_ids

  subnet_id                 = each.value
  network_security_group_id = azurerm_network_security_group.this.id
}
