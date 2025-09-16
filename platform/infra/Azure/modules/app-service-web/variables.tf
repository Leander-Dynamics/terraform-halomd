variable "name" {
  description = "Name of the App Service instance."
  type        = string
}

variable "plan_name" {
  description = "Name assigned to the App Service plan."
  type        = string
}

variable "plan_sku" {
  description = "SKU applied to the App Service plan (for example P1v3, S1)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the App Service resources."
  type        = string
}

variable "location" {
  description = "Azure region where the App Service resources are deployed."
  type        = string
}

variable "dotnet_version" {
  description = ".NET runtime version applied to the Linux web app."
  type        = string
  default     = "8.0"
}

variable "https_only" {
  description = "Force HTTPS traffic only for the web app."
  type        = bool
  default     = true
}

variable "always_on" {
  description = "Enable the Always On setting for the web app."
  type        = bool
  default     = true
}

variable "ftps_state" {
  description = "FTPS configuration for the web app."
  type        = string
  default     = "Disabled"
}

variable "run_from_package" {
  description = "When true, sets WEBSITE_RUN_FROM_PACKAGE to 1."
  type        = bool
  default     = false
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string injected into the web app settings."
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "Optional Log Analytics workspace resource ID used for diagnostics."
  type        = string
  default     = null
}

variable "app_settings" {
  description = "Additional application settings merged into the web app configuration."
  type        = map(string)
  default     = {}
}

variable "connection_strings" {
  description = "Connection string definitions exposed to the web app."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "tags" {
  description = "Tags applied to created resources."
  type        = map(string)
  default     = {}
}
