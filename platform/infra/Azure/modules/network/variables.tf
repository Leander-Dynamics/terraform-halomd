variable "name" {
  description = "Name of the virtual network."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the network."
  type        = string
}

variable "location" {
  description = "Azure region for the network."
  type        = string
}

variable "address_space" {
  description = "Address space assigned to the virtual network."
  type        = list(string)
}

variable "dns_servers" {
  description = "Optional list of custom DNS servers."
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

variable "tags" {
  description = "Tags applied to the network resources."
  type        = map(string)
  default     = {}
}
