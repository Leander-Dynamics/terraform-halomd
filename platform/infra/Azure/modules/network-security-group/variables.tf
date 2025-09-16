variable "name" {
  description = "Name of the Network Security Group."
  type        = string
}

variable "location" {
  description = "Azure region where the NSG will be deployed."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group in which the NSG will be created."
  type        = string
}

variable "security_rules" {
  description = <<-DOC
  Map of security rule definitions keyed by rule name. Each rule supports the
  following attributes:
    - priority (number, required) – evaluation order where lower numbers are processed first.
    - direction (string, optional, defaults to "Inbound").
    - access (string, optional, defaults to "Allow").
    - protocol (string, optional, defaults to "*").
    - source_port_range (string, optional).
    - source_port_ranges (list(string), optional) – mutually exclusive with source_port_range.
    - destination_port_range (string, optional).
    - destination_port_ranges (list(string), optional) – mutually exclusive with destination_port_range.
    - source_address_prefix (string, optional).
    - source_address_prefixes (list(string), optional) – mutually exclusive with source_address_prefix.
    - destination_address_prefix (string, optional).
    - destination_address_prefixes (list(string), optional) – mutually exclusive with destination_address_prefix.
    - description (string, optional) – free-form documentation for the rule.

  When no rules are supplied the NSG will contain only the default Azure rules.
  DOC

  type = map(object({
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
  }))
  default = {}
}

variable "subnet_ids" {
  description = "Set of subnet resource IDs to associate with this network security group."
  type        = set(string)
  default     = []
}

variable "tags" {
  description = "Tags to apply to the NSG."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Optional tags to apply to the network security group."
  type        = map(string)
  default     = {}
}
