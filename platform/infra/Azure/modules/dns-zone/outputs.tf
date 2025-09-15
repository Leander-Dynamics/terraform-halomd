output "zone_id" {
  description = "Resource ID of the DNS zone."
  value       = azurerm_dns_zone.this.id
}

output "zone_name" {
  description = "Name of the DNS zone."
  value       = azurerm_dns_zone.this.name
}

output "a_record_fqdns" {
  description = "Map of created A record FQDNs keyed by record name."
  value = {
    for name, record in azurerm_dns_a_record.this :
    name => record.fqdn
  }
}

output "cname_record_fqdns" {
  description = "Map of created CNAME record FQDNs keyed by record name."
  value = {
    for name, record in azurerm_dns_cname_record.this :
    name => record.fqdn
  }
}
