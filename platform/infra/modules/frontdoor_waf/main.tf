resource "azurerm_cdn_frontdoor_profile" "this" {
  name                = var.profile_name
  resource_group_name = var.resource_group_name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = var.tags
}

resource "azurerm_cdn_frontdoor_endpoint" "this" {
  name                     = var.endpoint_name
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.this.id
}

resource "azurerm_web_application_firewall_policy" "this" {
  name                = "${var.profile_name}-waf"
  resource_group_name = var.resource_group_name
  location            = "global"
  policy_settings {
    enabled            = true
    mode               = "Prevention"
    request_body_check = true
  }
  managed_rules {
    managed_rule_set {
      type    = "OWASP"
      version = "3.2"
    }
  }
  tags = var.tags
}

resource "azurerm_cdn_frontdoor_origin_group" "this" {
  name                     = "origin-group"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.this.id
  load_balancing {
    sample_size                        = 4
    successful_samples_required        = 3
    additional_latency_in_milliseconds = 0
  }
}

resource "azurerm_cdn_frontdoor_origin" "this" {
  name                           = "origin"
  cdn_frontdoor_origin_group_id  = azurerm_cdn_frontdoor_origin_group.this.id
  enabled                        = true
  certificate_name_check_enabled = false
  host_name                      = var.origin_host_name
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = var.origin_host_name
  priority                       = 1
  weight                         = 1000
}

resource "azurerm_cdn_frontdoor_route" "this" {
  name                          = "route-all"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.this.id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.this.id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.this.id]
  https_redirect_enabled        = true
  forwarding_protocol           = "HttpsOnly"
  patterns_to_match             = ["/*"]
  supported_protocols           = ["Http", "Https"]
  link_to_default_domain        = true
  cache {
    query_string_caching_behavior = "IgnoreQueryString"
  }
}

// Note: Attaching WAF to Front Door Standard/Premium occurs via security policy. This is left as a follow-up
// to avoid schema drift across provider versions. The WAF policy is provisioned and can be attached later.
