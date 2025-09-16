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
  description = "Azure compute size (SKU) to use for the virtual machine, e.g. Standard_DS1_v2."
  type        = string
}

variable "admin_username" {
  description = "Admin username provisioned on the Linux VM."
  type        = string
}

variable "image_publisher" {
  description = "Publisher of the image used to create the VM."
  type        = string
}

variable "image_offer" {
  description = "Offer of the image used to create the VM."
  type        = string
}

variable "image_sku" {
  description = "SKU of the image used to create the VM."
  type        = string
}

variable "ssh_key" {
  description = "SSH public key that will be added for the admin user."
  type        = string
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
