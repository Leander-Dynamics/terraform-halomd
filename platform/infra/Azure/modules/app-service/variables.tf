variable "plan_name" {
  description = "Name of the App Service plan."
  type        = string
}

variable "plan_sku" {
  description = "SKU for the App Service plan (e.g. P1v3, S1)."
  type        = string
}

variable "plan_os_type" {
  description = "Operating system for the App Service plan."
  type        = string
  default     = "Windows"
}

variable "app_name" {
  description = "Name of the App Service."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the App Service."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "https_only" {
  description = "Force HTTPS traffic only."
  type        = bool
  default     = true
}

variable "always_on" {
  description = "Ensure the app stays warm."
  type        = bool
  default     = true
}

variable "app_settings" {
  description = "Map of application settings."
  type        = map(string)
  default     = {}
}

variable "connection_strings" {
  description = "Map of connection string definitions."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "tags" {
  description = "Tags to apply to all resources."
  type        = map(string)
  default     = {}
}
