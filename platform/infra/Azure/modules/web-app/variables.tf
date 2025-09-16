# -------------------------
# App Service Plan
# -------------------------

variable "plan_name" {
  description = "Name of the App Service plan."
  type        = string
}

variable "plan_sku" {
  description = "SKU used by the App Service plan."
  type        = string
  default     = "B1"
}

# -------------------------
# App Service Web App
# -------------------------

variable "name" {
  description = "Name of the App Service."
  type        = string
}

variable "app_name" {
  description = "Alias for name; used for diagnostic output naming."
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
  description = "Runtime stack for the Linux web app. Supported values: dotnet, node, python."
  type        = string
  default     = "dotnet"

  validation {
    condition     = contains(["dotnet", "node", "python"], lower(trimspace(coalesce(var.runtime_stack, "dotnet"))))
    error_message = "runtime_stack must be one of: dotnet, node, python."
  }
}

variable "runtime_version" {
  description = "Runtime version associated with the selected stack."
  type        = string
  default     = "8.0"
}

variable "always_on" {
  description = "Enables the Always On setting for the web app."
  type        = bool
  default     = true
}

variable "https_only" {
  description = "Force HTTPS traffic to the web app."
  type        = bool
  default     = true
}

variable "ftps_state" {
  description = "FTPS configuration for the web app."
  type        = string
  default     = "Disabled"
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string injected into the app settings."
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace resource ID for diagnostics."
  type        = string
  default     = null
}

variable "run_from_package" {
  description = "When true, sets WEBSITE_RUN_FROM_PACKAGE to 1."
  type        = bool
  default     = null
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

variable "tags" {
  description = "Tags applied to created resources."
  type        = map(string)
  default     = {}
}
