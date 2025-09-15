variable "name" {
  description = "Name of the App Service."
  type        = string
}

variable "plan_name" {
  description = "Name of the App Service plan."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group where the App Service will be created."
  type        = string
}

variable "location" {
  description = "Azure region for the App Service."
  type        = string
}

variable "plan_sku" {
  description = "SKU for the App Service plan."
  type        = string
}

variable "dotnet_version" {
  description = ".NET version to use for the App Service runtime."
  type        = string
  default     = "8.0"
}

variable "https_only" {
  description = "Forces HTTPS-only traffic when true."
  type        = bool
  default     = true
}

variable "always_on" {
  description = "Whether the App Service should always be on."
  type        = bool
  default     = true
}

variable "identity_type" {
  description = "Managed identity type for the App Service."
  type        = string
  default     = "SystemAssigned"
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string. When set, automatically added to app settings."
  type        = string
  default     = null
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace ID for diagnostics."
  type        = string
  default     = null
}

variable "app_settings" {
  description = "Additional app settings for the App Service."
  type        = map(string)
  default     = {}
}

variable "connection_strings" {
  description = "Connection strings to configure on the App Service."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "tags" {
  description = "Tags to apply to the App Service and plan."
  type        = map(string)
  default     = {}
}
