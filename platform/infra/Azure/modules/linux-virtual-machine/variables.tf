variable "name" {}
variable "location" {}
variable "resource_group_name" {}
variable "sku" { default = "Standard" }
variable "nic_id" {
  description = "Resource ID of the primary network interface to attach to the VM."
  type        = string

  validation {
    condition     = can(regex("^/subscriptions/[^/]+/resourceGroups/[^/]+/providers/Microsoft\\.Network/networkInterfaces/[^/]+$", trimspace(var.nic_id)))
    error_message = "nic_id must be a valid Azure resource ID for a network interface."
  }
}
