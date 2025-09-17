variable "name" {
  description = "Name of the managed disk."
  type        = string
}

variable "location" {
  description = "Azure region where the managed disk will be deployed."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group in which the managed disk will be created."
  type        = string
}

variable "disk_size_gb" {
  description = "Size of the managed disk in GB."
  type        = number
}

variable "storage_account_type" {
  description = "Specifies the storage account type for the managed disk (e.g. Standard_LRS, Premium_LRS)."
  type        = string
  default     = "Standard_LRS"
}
