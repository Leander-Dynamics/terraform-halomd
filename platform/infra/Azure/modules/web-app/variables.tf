variable "name" {
  type = string
}

variable "plan_name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "plan_sku" {
  type    = string
  default = "B1"
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
  description = "Runtime version that corresponds to the selected runtime stack."
  type        = string
  default     = "8.0"
}

variable "always_on" {
  description = "Enables the Always On setting for the web app."
  type        = bool
  default     = false
}

variable "app_insights_connection_string" {
  type = string
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace resource ID for diagnostic settings."
  type        = string
  default     = null
}

variable "run_from_package" {
  description = "When true, sets WEBSITE_RUN_FROM_PACKAGE to 1."
  type        = bool
  default     = null
}

variable "app_settings" {
  type    = map(string)
  default = {}
}

variable "connection_strings" {
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "tags" {
  type    = map(string)
  default = {}
}
