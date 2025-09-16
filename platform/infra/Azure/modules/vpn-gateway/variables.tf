variable "name" {
  description = "Name of the virtual network gateway."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the gateway."
  type        = string
}

variable "location" {
  description = "Azure region for the gateway."
  type        = string
}

variable "gateway_subnet_id" {
  description = "Subnet ID for the dedicated GatewaySubnet."
  type        = string
}

variable "gateway_type" {
  description = "Gateway type, typically 'Vpn' or 'ExpressRoute'."
  type        = string
  default     = "Vpn"
}

variable "sku" {
  description = "SKU of the virtual network gateway."
  type        = string
}

variable "vpn_type" {
  description = "VPN routing type (PolicyBased or RouteBased)."
  type        = string
  default     = "RouteBased"
}

variable "active_active" {
  description = "Deploy the gateway in active-active mode."
  type        = bool
  default     = false
}

variable "enable_bgp" {
  description = "Enable BGP on the gateway."
  type        = bool
  default     = false
}

variable "generation" {
  description = "Gateway generation when applicable (Generation1, Generation2)."
  type        = string
  default     = null
}

variable "ip_configuration_name" {
  description = "Name assigned to the IP configuration block."
  type        = string
  default     = "default"
}

variable "custom_route_address_prefixes" {
  description = "Custom route address prefixes propagated to connected networks."
  type        = list(string)
  default     = []
}

variable "public_ip_configuration" {
  description = "Configuration for the public IP created for the gateway."
  type = object({
    name              = string
    allocation_method = optional(string, "Static")
    sku               = optional(string, "Standard")
    zones             = optional(list(string), [])
    tags              = optional(map(string), {})
  })
  default = null
}

variable "public_ip_id" {
  description = "Existing public IP ID to associate with the gateway."
  type        = string
  default     = null

  validation {
    condition = (
      (var.public_ip_configuration != null ? 1 : 0) +
      (var.public_ip_id != null && trim(var.public_ip_id) != "" ? 1 : 0)
    ) > 0
    error_message = "Either public_ip_configuration or public_ip_id must be provided to associate a public IP with the gateway."
  }
}

variable "vpn_client_configuration" {
  description = "Optional VPN client configuration details."
  type = object({
    address_space         = list(string)
    vpn_client_protocols  = optional(list(string), ["OpenVPN"])
    vpn_auth_types        = optional(list(string), [])
    aad_tenant            = optional(string)
    aad_audience          = optional(string)
    aad_issuer            = optional(string)
    radius_server_address = optional(string)
    radius_server_secret  = optional(string)
    root_certificates = optional(list(object({
      name             = string
      public_cert_data = string
    })), [])
    revoked_certificates = optional(list(object({
      name       = string
      thumbprint = string
    })), [])
  })
  default = null
}

variable "bgp_settings" {
  description = "Optional BGP configuration for the gateway."
  type = object({
    asn         = number
    peer_weight = optional(number)
    peering_addresses = optional(list(object({
      ip_configuration_name = string
      apipa_addresses       = list(string)
    })), [])
  })
  default = null
}

variable "tags" {
  description = "Tags applied to the gateway resources."
  type        = map(string)
  default     = {}
}
