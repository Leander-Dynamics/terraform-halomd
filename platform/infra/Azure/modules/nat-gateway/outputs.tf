output "id" {
  description = "Resource ID of the NAT Gateway."
  value       = azurerm_nat_gateway.this.id
}

output "name" {
  description = "Name of the NAT Gateway."
  value       = azurerm_nat_gateway.this.name
}

output "public_ip_ids" {
  description = "Combined list of public IP IDs associated with the NAT Gateway."
  value       = local.effective_public_ip_ids
}

output "created_public_ip_ids" {
  description = "Map of public IP IDs created by the module keyed by name."
  value       = { for name, pip in azurerm_public_ip.this : name => pip.id }
}

output "created_public_ip_addresses" {
  description = "Map of public IP addresses created by the module keyed by name."
  value       = { for name, pip in azurerm_public_ip.this : name => pip.ip_address }
}

output "subnet_association_ids" {
  description = "Map of subnet NAT Gateway association IDs keyed by subnet ID."
  value       = { for subnet_id, assoc in azurerm_subnet_nat_gateway_association.this : subnet_id => assoc.id }
}
