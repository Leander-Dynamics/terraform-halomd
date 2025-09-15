output "zone_name" {
  description = "DNS zone name."
  value       = azurerm_dns_zone.this.name
}

output "zone_id" {
  description = "Resource ID of the DNS zone."
  value       = azurerm_dns_zone.this.id
}

output "name_servers" {
  description = "Azure-assigned name servers for the DNS zone."
  value       = azurerm_dns_zone.this.name_servers
}
