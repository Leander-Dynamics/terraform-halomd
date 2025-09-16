output "id" {
  description = "Resource ID of the private endpoint."
  value       = azurerm_private_endpoint.this.id
}

output "name" {
  description = "Name of the private endpoint resource."
  value       = azurerm_private_endpoint.this.name
}

output "network_interface_ids" {
  description = "Identifiers of the network interfaces created for the private endpoint."
  value       = azurerm_private_endpoint.this.network_interface_ids
}

output "subnet_id" {
  description = "Subnet ID supplied to the private endpoint."
  value       = var.subnet_id
}
