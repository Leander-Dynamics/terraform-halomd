variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "dns_prefix" {
  type    = string
}

variable "node_count" {
  type    = number
  default = 1
}

variable "vm_size" {
  type    = string
  default = "Standard_DS2_v2"
}

variable "identity_type" {
  description = "Managed identity configuration applied to the AKS cluster."
  type        = string
  default     = "SystemAssigned"

  validation {
    condition = contains([
      "SystemAssigned",
      "UserAssigned",
      "SystemAssigned,UserAssigned",
    ], var.identity_type)
    error_message = "identity_type must be one of SystemAssigned, UserAssigned, or SystemAssigned,UserAssigned."
  }
}

variable "identity_ids" {
  description = "User-assigned identity resource IDs when identity_type includes UserAssigned."
  type        = list(string)
  default     = []

  validation {
    condition = contains([
      "UserAssigned",
      "SystemAssigned,UserAssigned",
    ], var.identity_type) ? length(var.identity_ids) > 0 : true
    error_message = "At least one identity ID must be supplied when using a UserAssigned identity type."
  }
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace resource ID used for Container Insights."
  type        = string
  default     = null
}
