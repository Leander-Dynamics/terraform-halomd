variable "name" {
  description = "Name of the NAT Gateway resource."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group in which the NAT Gateway will be created."
  type        = string
}

variable "location" {
  description = "Azure region for the NAT Gateway."
  type        = string
}

variable "sku_name" {
  description = "SKU name for the NAT Gateway."
  type        = string
  default     = "Standard"
}

variable "idle_timeout_in_minutes" {
  description = "Idle timeout, in minutes, for outbound connections."
  type        = number
  default     = 4
}

variable "zones" {
  description = "Availability zones to pin the NAT Gateway to."
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Tags applied to the NAT Gateway resources."
  type        = map(string)
  default     = {}
}

variable "public_ip_configurations" {
  description = "Definitions for public IP addresses created for the NAT Gateway."
  type = list(object({
    name              = string
    allocation_method = optional(string, "Static")
    sku               = optional(string, "Standard")
    zones             = optional(list(string), [])
    tags              = optional(map(string), {})
  }))
  default = []
}

variable "public_ip_ids" {
  description = "Existing public IP resource IDs associated with the NAT Gateway."
  type        = list(string)
  default     = []
}

variable "subnet_ids" {
  description = "Subnets that should be associated with the NAT Gateway."
  type        = list(string)
  default     = []
}
