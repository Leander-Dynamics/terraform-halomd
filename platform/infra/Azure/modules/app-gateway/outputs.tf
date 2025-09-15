output "id" {
  description = "ID of the Application Gateway."
  value       = azurerm_application_gateway.this.id
}

output "name" {
  description = "Name of the Application Gateway."
  value       = azurerm_application_gateway.this.name
}

output "public_ip_id" {
  description = "ID of the public IP associated with the Application Gateway."
  value       = azurerm_public_ip.this.id
}

output "public_ip_address" {
  description = "Allocated public IP address for the Application Gateway."
  value       = azurerm_public_ip.this.ip_address
}

output "backend_address_pool_id" {
  description = "ID of the default backend address pool."
  value       = azurerm_application_gateway.this.backend_address_pool[0].id
}
