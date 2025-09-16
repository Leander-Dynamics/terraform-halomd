# -------------------------
# Bastion
# -------------------------
variable "enable_bastion" {
  description = "Flag to deploy an Azure Bastion host."
  type        = bool
  default     = false
}

variable "bastion_subnet_key" {
  description = "Key referencing the AzureBastionSubnet entry in the `subnets` map."
  type        = string
  default     = null

  validation {
    condition     = var.enable_bastion == false || try(trimspace(var.bastion_subnet_key), "") != ""
    error_message = "bastion_subnet_key must be provided when enable_bastion is true."
  }
}

# -------------------------
# Networking
# -------------------------
variable "vnet_address_space" {
  description = "Address space assigned to the virtual network."
  type        = list(string)
}

variable "vnet_dns_servers" {
  description = "Optional custom DNS servers applied to the virtual network."
  type        = list(string)
  default     = []
}

variable "subnets" {
  description = "Map of subnet definitions keyed by subnet name."
  type = map(object({
    address_prefixes  = list(string)
    service_endpoints = optional(list(string), [])
    delegations = optional(list(object({
      name = string
      service_delegation = object({
        name    = string
        actions = list(string)
      })
    })), [])
  }))
}

variable "subnet_network_security_rules" {
  description = <<-DOC
  Map of network security rule sets keyed by subnet name. Each entry should
  match the `security_rules` input for the `network-security-group` module and
  defaults to an empty map, resulting in only the built-in Azure NSG rules.
  DOC
  type = map(map(object({
    priority                     = number
    direction                    = optional(string, "Inbound")
    access                       = optional(string, "Allow")
    protocol                     = optional(string, "*")
    source_port_range            = optional(string)
    source_port_ranges           = optional(list(string))
    destination_port_range       = optional(string)
    destination_port_ranges      = optional(list(string))
    source_address_prefix        = optional(string)
    source_address_prefixes      = optional(list(string))
    destination_address_prefix   = optional(string)
    destination_address_prefixes = optional(list(string))
    description                  = optional(string)
  })))
  default = {}
}

variable "nat_gateway_configuration" {
  description = "Configuration for the NAT Gateway when enabled."
  type = object({
    name                     = string
    subnet_keys              = list(string)
    public_ip_configurations = optional(list(object({
      name              = string
      allocation_method = optional(string, "Static")
      sku               = optional(string, "Standard")
      zones             = optional(list(string), [])
      tags              = optional(map(string), {})
    })), [])
    public_ip_ids           = optional(list(string), [])
    sku_name                = optional(string, "Standard")
    idle_timeout_in_minutes = optional(number, 4)
    zones                   = optional(list(string), [])
    tags                    = optional(map(string), {})
  })
  default = null

  validation {
    condition = var.nat_gateway_configuration == null ? true : (
      length(try(var.nat_gateway_configuration.public_ip_configurations, [])) +
      length(try(var.nat_gateway_configuration.public_ip_ids, []))
    ) > 0
    error_message = "At least one public IP configuration or existing public IP ID must be provided for the NAT Gateway."
  }

  validation {
    condition     = var.nat_gateway_configuration == null ? true : length(var.nat_gateway_configuration.subnet_keys) > 0
    error_message = "At least one subnet key must be provided to associate the NAT Gateway."
  }
}

variable "vpn_gateway_configuration" {
  description = "Configuration for the virtual network gateway when enabled."
  type = object({
    name                 = string
    gateway_subnet_key   = string
    sku                  = string
    gateway_type         = optional(string, "Vpn")
    vpn_type             = optional(string, "RouteBased")
    active_active        = optional(bool, false)
    enable_bgp           = optional(bool, false)
    generation           = optional(string)
    ip_configuration_name = optional(string, "default")
    custom_routes        = optional(list(string), [])
    public_ip = optional(object({
      name              = string
      allocation_method = optional(string, "Static")
      sku               = optional(string, "Standard")
      zones             = optional(list(string), [])
      tags              = optional(map(string), {})
    }))
    public_ip_id            = optional(string)
    vpn_client_configuration = optional(object({
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
    }))
    bgp_settings = optional(object({
      asn         = number
      peer_weight = optional(number)
      peering_addresses = optional(list(object({
        ip_configuration_name = string
        apipa_addresses       = list(string)
      })), [])
    }))
    tags = optional(map(string), {})
  })
  default = null

  validation {
    condition = var.vpn_gateway_configuration == null ? true : (
      (try(var.vpn_gateway_configuration.public_ip, null) != null ? 1 : 0) +
      (try(var.vpn_gateway_configuration.public_ip_id, null) != null && try(trim(var.vpn_gateway_configuration.public_ip_id), "") != "" ? 1 : 0)
    ) > 0
    error_message = "Either a public_ip definition or public_ip_id must be supplied for the virtual network gateway."
  }
}
