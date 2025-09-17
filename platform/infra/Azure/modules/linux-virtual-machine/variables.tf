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

variable "size" {
  description = "Size of the virtual machine (for example, Standard_B2s)."
  type        = string
}

variable "admin_username" {
  description = "Administrator username for the virtual machine."
  type        = string
}

variable "admin_password" {
  description = "Optional administrator password for the virtual machine. If omitted, SSH key authentication must be provided."
  type        = string
  default     = null
  sensitive   = true
}

variable "ssh_key" {
  description = "Optional SSH public key used for administrator authentication."
  type        = string
  default     = null
}

variable "image_publisher" {
  description = "Publisher of the operating system image."
  type        = string
}

variable "image_offer" {
  description = "Offer of the operating system image."
  type        = string
}

variable "image_sku" {
  description = "SKU of the operating system image."
  type        = string
}

variable "image_version" {
  description = "Version of the operating system image."
  type        = string
  default     = "latest"
}

variable "nic_id" {
  description = "Resource ID of the primary network interface to attach to the VM."
  type        = string

  validation {
    condition     = var.nic_id == null || trimspace(var.nic_id) != ""
    error_message = "nic_id must be provided."
  }

  validation {
    condition     = var.nic_id == null || can(regex("^/subscriptions/[^/]+/resourceGroups/[^/]+/providers/Microsoft\\.Network/networkInterfaces/[^/]+$", trimspace(var.nic_id)))
    error_message = "nic_id must be a valid Azure resource ID for a network interface."
  }
}
