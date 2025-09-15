variable "resource_group_name" {
  description = "Resource group where Application Insights and Log Analytics will be created."
  type        = string
}

variable "location" {
  description = "Azure region for monitoring resources."
  type        = string
}

variable "log_analytics_workspace_name" {
  description = "Name of the Log Analytics workspace."
  type        = string
}

variable "application_insights_name" {
  description = "Name of the Application Insights component."
  type        = string
}

variable "application_type" {
  description = "Application Insights application type."
  type        = string
  default     = "web"
}

variable "log_analytics_sku" {
  description = "Pricing SKU for the Log Analytics workspace."
  type        = string
  default     = "PerGB2018"
}

variable "log_analytics_retention_in_days" {
  description = "Retention in days for the Log Analytics workspace."
  type        = number
  default     = 30
}

variable "log_analytics_daily_quota_gb" {
  description = "Daily data cap in GB for the Log Analytics workspace (-1 for unlimited)."
  type        = number
  default     = -1
}

variable "tags" {
  description = "Tags to apply to monitoring resources."
  type        = map(string)
  default     = {}
}
