variable "name" {
  description = "Name of the Function App."
  type        = string
}

variable "location" {
  description = "Azure region where the Function App will be deployed."
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group hosting the Function App."
  type        = string
}

variable "plan_name" {
  description = "Name of the App Service plan used by the Function App."
  type        = string
}

variable "plan_sku" {
  description = "SKU for the App Service plan."
  type        = string
  default     = "Y1"
}

variable "storage_account_name" {
  description = "Optional name of the storage account used by the Function App. Leave empty to generate a unique name."
  type        = string
  default     = ""

  validation {
    condition     = var.storage_account_name == "" ? true : can(regex("^[a-z0-9]{3,24}$", var.storage_account_name))
    error_message = "storage_account_name must be empty or contain 3-24 lowercase letters and numbers."
  }
}

variable "storage_account_tier" {
  description = "Tier of the storage account backing the Function App."
  type        = string
  default     = "Standard"
}

variable "storage_account_replication_type" {
  description = "Replication type for the storage account backing the Function App."
  type        = string
  default     = "LRS"
}

variable "functions_extension_version" {
  description = "Runtime extension version for the Function App."
  type        = string
  default     = "~4"
}

variable "runtime_stack" {
  description = "Worker runtime stack for the Function App."
  type        = string
  default     = "dotnet"

  validation {
    condition     = contains(["dotnet", "node", "python"], lower(trimspace(var.runtime_stack)))
    error_message = "runtime_stack must be one of: dotnet, node, python."
  }
}

variable "runtime_version" {
  description = "Optional runtime version for the selected worker stack."
  type        = string
  default     = ""
}

variable "application_insights_connection_string" {
  description = "Connection string for Application Insights instrumentation."
  type        = string
  default     = ""
}

variable "log_analytics_workspace_id" {
  description = "Resource ID of the Log Analytics workspace for diagnostics."
  type        = string
  default     = null
}

variable "app_settings" {
  description = "Additional application settings for the Function App."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Resource tags to apply to created resources."
  type        = map(string)
  default     = {}
}
