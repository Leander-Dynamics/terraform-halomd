output "id" {
  description = "Application Gateway resource ID."
  value       = azurerm_application_gateway.this.id
}

output "public_ip_id" {
  description = "Public IP resource ID."
  value       = azurerm_public_ip.this.id
}

output "public_ip_fqdn" {
  description = "Public FQDN assigned to the Application Gateway."
  value       = azurerm_public_ip.this.fqdn
}
