output "id" {
  description = "Resource ID of the virtual network gateway."
  value       = azurerm_virtual_network_gateway.this.id
}

output "name" {
  description = "Name of the virtual network gateway."
  value       = azurerm_virtual_network_gateway.this.name
}

output "public_ip_id" {
  description = "Effective public IP resource ID used by the gateway."
  value       = local.effective_public_ip_id
}

output "public_ip_address" {
  description = "Public IP address allocated when the module creates it."
  value       = local.public_ip_configuration == null ? null : azurerm_public_ip.this[0].ip_address
}

output "bgp_settings" {
  description = "Effective BGP settings applied to the gateway."
  value       = var.bgp_settings
}

output "vpn_client_configuration" {
  description = "VPN client configuration supplied to the gateway."
  value       = var.vpn_client_configuration
}
