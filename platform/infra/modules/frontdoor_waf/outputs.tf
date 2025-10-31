output "profile_id" {
  description = "Front Door profile ID"
  value       = azurerm_cdn_frontdoor_profile.this.id
}

output "endpoint_hostname" {
  description = "Front Door default endpoint hostname"
  value       = azurerm_cdn_frontdoor_endpoint.this.host_name
}

output "waf_policy_id" {
  description = "Web Application Firewall policy ID"
  value       = azurerm_web_application_firewall_policy.this.id
}
