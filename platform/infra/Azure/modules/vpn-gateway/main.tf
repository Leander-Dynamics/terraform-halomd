locals {
  public_ip_configuration = var.public_ip_configuration == null ? null : {
    name              = var.public_ip_configuration.name
    allocation_method = try(var.public_ip_configuration.allocation_method, "Static")
    sku               = try(var.public_ip_configuration.sku, "Standard")
    zones             = try(var.public_ip_configuration.zones, [])
    tags              = try(var.public_ip_configuration.tags, {})
  }
}

resource "azurerm_public_ip" "this" {
  count               = local.public_ip_configuration == null ? 0 : 1
  name                = local.public_ip_configuration.name
  location            = var.location
  resource_group_name = var.resource_group_name
  allocation_method   = local.public_ip_configuration.allocation_method
  sku                 = local.public_ip_configuration.sku
  zones               = length(local.public_ip_configuration.zones) > 0 ? local.public_ip_configuration.zones : null
  tags                = merge(var.tags, local.public_ip_configuration.tags)
}

locals {
  provided_public_ip_id = try(trim(var.public_ip_id), "") != "" ? var.public_ip_id : null
  created_public_ip_id  = local.public_ip_configuration == null ? null : azurerm_public_ip.this[0].id
  effective_public_ip_id = coalesce(local.provided_public_ip_id, local.created_public_ip_id)
}

resource "azurerm_virtual_network_gateway" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  type                = var.gateway_type
  vpn_type            = var.vpn_type
  active_active       = var.active_active
  enable_bgp          = var.enable_bgp
  sku                 = var.sku
  generation          = var.generation
  tags                = var.tags

  ip_configuration {
    name                          = var.ip_configuration_name
    public_ip_address_id          = local.effective_public_ip_id
    private_ip_address_allocation = "Dynamic"
    subnet_id                     = var.gateway_subnet_id
  }

  dynamic "custom_route" {
    for_each = length(var.custom_route_address_prefixes) > 0 ? [1] : []
    content {
      address_prefixes = var.custom_route_address_prefixes
    }
  }

  dynamic "vpn_client_configuration" {
    for_each = var.vpn_client_configuration != null ? [var.vpn_client_configuration] : []
    content {
      address_space         = vpn_client_configuration.value.address_space
      vpn_client_protocols  = try(vpn_client_configuration.value.vpn_client_protocols, ["OpenVPN"])
      vpn_auth_types        = try(vpn_client_configuration.value.vpn_auth_types, null)
      aad_tenant            = try(vpn_client_configuration.value.aad_tenant, null)
      aad_audience          = try(vpn_client_configuration.value.aad_audience, null)
      aad_issuer            = try(vpn_client_configuration.value.aad_issuer, null)
      radius_server_address = try(vpn_client_configuration.value.radius_server_address, null)
      radius_server_secret  = try(vpn_client_configuration.value.radius_server_secret, null)

      dynamic "root_certificate" {
        for_each = try(vpn_client_configuration.value.root_certificates, [])
        content {
          name             = root_certificate.value.name
          public_cert_data = root_certificate.value.public_cert_data
        }
      }

      dynamic "revoked_certificate" {
        for_each = try(vpn_client_configuration.value.revoked_certificates, [])
        content {
          name       = revoked_certificate.value.name
          thumbprint = revoked_certificate.value.thumbprint
        }
      }
    }
  }

  dynamic "bgp_settings" {
    for_each = var.bgp_settings != null ? [var.bgp_settings] : []
    content {
      asn         = bgp_settings.value.asn
      peer_weight = try(bgp_settings.value.peer_weight, null)

      dynamic "peering_addresses" {
        for_each = try(bgp_settings.value.peering_addresses, [])
        content {
          ip_configuration_name = peering_addresses.value.ip_configuration_name
          apipa_addresses       = peering_addresses.value.apipa_addresses
        }
      }
    }
  }
}
