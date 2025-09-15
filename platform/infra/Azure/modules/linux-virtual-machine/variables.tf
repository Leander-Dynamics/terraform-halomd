variable "name" {
  description = "Name of the virtual machine."
  type        = string
}

variable "location" {
  description = "Azure region where the VM will be deployed."
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group in which to deploy the VM."
  type        = string
}

variable "sku" {
  description = "SKU/size of the virtual machine."
  type        = string
  default     = "Standard"
}

variable "nic_id" {
  description = "Resource ID of the primary network interface to attach to the VM."
  type        = string
  default     = ""

  validation {
    condition     = var.nic_id == "" || can(regex("^/subscriptions/[^/]+/resourceGroups/[^/]+/providers/Microsoft\\.Network/networkInterfaces/[^/]+$", trimspace(var.nic_id)))
    error_message = "nic_id must be empty or a valid Azure resource ID for a network interface."
  }
}
