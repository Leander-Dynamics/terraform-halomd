variable "plan_name" {
  description = "Name of the App Service plan."
  type        = string
}

variable "plan_sku" {
  description = "SKU used by the App Service plan."
  type        = string
}

variable "app_name" {
  description = "Name of the App Service."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the App Service resources."
  type        = string
}

variable "location" {
  description = "Azure region for the App Service resources."
  type        = string
}

variable "runtime_stack" {
  description = "Application runtime stack (dotnet, node, python)."
  type        = string
  default     = "dotnet"
}

variable "runtime_version" {
  description = "Runtime version associated with the selected stack."
  type        = string
  default     = "8.0"
}

variable "app_settings" {
  description = "Map of application settings applied to the web app."
  type        = map(string)
  default     = {}
}

variable "connection_strings" {
  description = "Map of connection string definitions keyed by name."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "log_analytics_workspace_id" {
  description = "Optional Log Analytics workspace resource ID for diagnostics."
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags applied to created resources."
  type        = map(string)
  default     = {}
}

variable "https_only" {
  description = "Force HTTPS traffic to the web app."
  type        = bool
  default     = true
}

variable "always_on" {
  description = "Keep the web app always on."
  type        = bool
  default     = true
}

variable "ftps_state" {
  description = "FTPS configuration for the web app."
  type        = string
  default     = "Disabled"
}
