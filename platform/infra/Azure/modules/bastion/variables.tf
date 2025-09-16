variable "name" {
  description = "Name of the Bastion host."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group where the Bastion host will be deployed."
  type        = string
}

variable "location" {
  description = "Azure region for the Bastion host."
  type        = string
}

variable "subnet_id" {
  description = "Identifier of the dedicated AzureBastionSubnet used by the host."
  type        = string
}

variable "tags" {
  description = "Tags to apply to the Bastion resources."
  type        = map(string)
  default     = {}
}

variable "sku" {
  description = "SKU tier for the Bastion host (Basic or Standard)."
  type        = string
  default     = "Standard"
}

variable "scale_units" {
  description = "Number of scale units allocated to the Bastion host (Standard SKU only)."
  type        = number
  default     = 2
}

variable "copy_paste_enabled" {
  description = "Enable copy and paste functionality through the Bastion session."
  type        = bool
  default     = true
}

variable "file_copy_enabled" {
  description = "Enable file copy support (requires the Standard SKU)."
  type        = bool
  default     = true
}

variable "ip_connect_enabled" {
  description = "Allow native client support via IP connect (requires the Standard SKU)."
  type        = bool
  default     = true
}

variable "shareable_link_enabled" {
  description = "Enable shareable link access (requires the Standard SKU)."
  type        = bool
  default     = true
}

variable "tunneling_enabled" {
  description = "Enable tunnelling features (requires the Standard SKU)."
  type        = bool
  default     = true
}

variable "public_ip_name" {
  description = "Optional name for the Bastion public IP resource. Defaults to \"<name>-pip\"."
  type        = string
  default     = null
}

variable "public_ip_sku" {
  description = "SKU for the Bastion public IP address."
  type        = string
  default     = "Standard"
}

variable "public_ip_allocation_method" {
  description = "Allocation method for the public IP address."
  type        = string
  default     = "Static"
}

variable "ip_configuration_name" {
  description = "Name applied to the Bastion IP configuration block."
  type        = string
  default     = "default"
}

variable "zones" {
  description = "Availability zones to pin the Bastion resources to."
  type        = list(string)
  default     = []
}
