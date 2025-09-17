variable "plan_name" {
  type        = string
  description = "Name of the App Service Plan"
}

variable "plan_sku" {
  type        = string
  description = "SKU of the App Service Plan"
}

variable "resource_group_name" {
  type        = string
  description = "Resource group name"
}

variable "location" {
  type        = string
  description = "Azure location"
}

variable "name" {
  type        = string
  description = "Name of the App Service (arbitration)"
}

variable "runtime_stack" {
  type        = string
  description = "Runtime stack (e.g., dotnet, node, java)"
}

variable "runtime_version" {
  type        = string
  description = "Runtime version (e.g., 8.0, 6.0)"
}

variable "app_settings" {
  type        = map(string)
  description = "App settings for the App Service"
  default     = {}
}

variable "connection_strings" {
  type = list(object({
    name  = string
    type  = string
    value = string
  }))
  description = "Connection strings for the App Service"
  default     = []
}

variable "tags" {
  type        = map(string)
  description = "Tags to apply to resources"
  default     = {}
}

variable "log_analytics_workspace_id" {
  type        = string
  description = "Log Analytics Workspace ID"
}
variable "app_insights_connection_string" {
  type        = string
  description = "Application Insights connection string to link App Service with monitoring"
}

variable "app_insights_instrumentation_key" {
  type        = string
  description = "Optional Application Insights instrumentation key for legacy integrations"
  default     = null
}
