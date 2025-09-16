variable "enable_nat_gateway" {
  description = "Flag to deploy a NAT Gateway for outbound connectivity."
  type        = bool
  default     = false

  validation {
    condition     = var.enable_nat_gateway == false || var.nat_gateway_configuration != null
    error_message = "nat_gateway_configuration must be provided when enable_nat_gateway is true."
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

# -------------------------
# Key Vault & Storage networking
# -------------------------
variable "kv_public_network_access" {
  description = "Allow public network access to the Key Vault."
  type        = bool
  default     = true
}

variable "kv_network_acls" {
  description = "Optional network ACL configuration for the Key Vault."
  type = object({
    bypass                     = optional(string)
    default_action             = optional(string)
    ip_rules                   = optional(list(string))
    virtual_network_subnet_ids = optional(list(string))
  })
  default = null
}

variable "enable_kv_private_endpoint" {
  description = "Toggle creation of a private endpoint for the Key Vault."
  type        = bool
  default     = false
}

variable "kv_private_endpoint_subnet_key" {
  description = "Subnet key used when creating the Key Vault private endpoint."
  type        = string
  default     = null
}

variable "kv_private_dns_zone_ids" {
  description = "Private DNS zone IDs linked to the Key Vault private endpoint."
  type        = list(string)
  default     = []
}

variable "kv_private_endpoint_resource_id" {
  description = "Override resource ID supplied to the Key Vault private endpoint module."
  type        = string
  default     = null
}

variable "enable_storage_private_endpoint" {
  description = "Toggle creation of a private endpoint for the storage account."
  type        = bool
  default     = false
}

variable "storage_private_endpoint_subnet_key" {
  description = "Subnet key used when creating the storage account private endpoint."
  type        = string
  default     = null
}

variable "storage_private_dns_zone_ids" {
  description = "Private DNS zone IDs linked to the storage account private endpoint."
  type        = list(string)
  default     = []
}

variable "storage_private_endpoint_subresource_names" {
  description = "Subresource names exposed through the storage account private endpoint."
  type        = list(string)
  default     = ["blob"]
}

variable "storage_account_private_connection_resource_id" {
  description = "Resource ID used by the storage account private endpoint connection."
  type        = string
  default     = null
}
