output "id" {
  description = "Resource ID of the Bastion host."
  value       = azurerm_bastion_host.this.id
}

output "name" {
  description = "Name of the Bastion host."
  value       = azurerm_bastion_host.this.name
}

output "public_ip_id" {
  description = "Resource ID of the public IP associated with the Bastion host."
  value       = azurerm_public_ip.this.id
}

output "public_ip_address" {
  description = "IP address assigned to the Bastion host."
  value       = azurerm_public_ip.this.ip_address
}

output "public_ip_fqdn" {
  description = "FQDN generated for the Bastion public IP."
  value       = azurerm_public_ip.this.fqdn
}
