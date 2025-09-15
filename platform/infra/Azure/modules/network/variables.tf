variable "name" {
  description = "Name of the virtual network."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group where the virtual network is created."
  type        = string
}

variable "location" {
  description = "Azure region for the virtual network."
  type        = string
}

variable "address_space" {
  description = "List of address spaces for the virtual network."
  type        = list(string)
}

variable "subnets" {
  description = "Map of subnets to create inside the virtual network."
  type = map(object({
    address_prefixes  = list(string)
    service_endpoints = optional(list(string), [])
  }))
  default = {}
}

variable "tags" {
  description = "Tags to apply to the virtual network."
  type        = map(string)
  default     = {}
}
