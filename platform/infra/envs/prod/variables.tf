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
