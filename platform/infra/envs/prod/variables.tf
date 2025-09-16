# -------------------------
# Monitoring
# -------------------------

variable "log_analytics_workspace_name" {
  description = "Name assigned to the Log Analytics workspace for this environment."
  type        = string
}

variable "application_insights_name" {
  description = "Name assigned to the Application Insights resource for this environment."
  type        = string
}

variable "log_analytics_retention_in_days" {
  description = "Number of days to retain data within the Log Analytics workspace."
  type        = number
}

variable "log_analytics_daily_quota_gb" {
  description = "Daily ingestion quota, in GB, for the Log Analytics workspace (-1 for unlimited)."
  type        = number
}

# -------------------------
# General settings
# -------------------------

variable "project_name" {
  description = "Short name of the project used when constructing resource names."
  type        = string
}

variable "env_name" {
  description = "Name of the deployment environment (e.g. dev, stage, prod)."
  type        = string
}

variable "location" {
  description = "Azure region where resources will be deployed."
  type        = string
}

variable "tags" {
  description = "Common tags applied to all resources."
  type        = map(string)
  default     = {}
}

variable "kv_cicd_principal_id" {
  description = "Optional object ID for the CI/CD principal that needs access to Key Vault secrets."
  type        = string
  default     = null
}

variable "subscription_id" {
  description = "Optional Azure subscription ID override when the authenticated context differs from the desired target."
  type        = string
  default     = null
}

variable "tenant_id" {
  description = "Optional Azure AD tenant ID override when the authenticated context differs from the desired target."
  type        = string
  default     = null
}

# -------------------------
# Application Gateway
# -------------------------

variable "app_gateway_subnet_key" {
  description = "Key referencing the subnet used by the Application Gateway when selecting from the virtual network map."
  type        = string
  default     = null
}

variable "app_gateway_subnet_id" {
  description = "Resource ID of the subnet used by the Application Gateway when referencing an existing subnet directly."
  type        = string
  default     = null
}

variable "app_gateway_fqdn_prefix" {
  description = "Prefix applied to DNS names associated with the Application Gateway."
  type        = string
  default     = null
}

variable "app_gateway_backend_fqdns" {
  description = "List of backend FQDNs configured on the Application Gateway."
  type        = list(string)
  default     = []
}

# -------------------------
# DNS
# -------------------------

variable "dns_zone_name" {
  description = "Name of the DNS zone hosting environment-specific records."
  type        = string
  default     = null
}

variable "dns_a_records" {
  description = "Map of DNS A record definitions keyed by record name."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "dns_cname_records" {
  description = "Map of DNS CNAME record definitions keyed by record name."
  type = map(object({
    ttl    = number
    record = string
  }))
  default = {}
}

# -------------------------
# App Services
# -------------------------

variable "app_service_plan_sku" {
  description = "SKU for the App Service plan hosting the primary web application."
  type        = string
  default     = "B1"
}

variable "app_service_dotnet_version" {
  description = ".NET runtime version for the primary web application."
  type        = string
  default     = "8.0"
}

variable "app_service_app_insights_connection_string" {
  description = "Application Insights connection string injected into the primary web app."
  type        = string

  validation {
    condition     = try(trimspace(var.app_service_app_insights_connection_string), "") != ""
    error_message = "app_service_app_insights_connection_string must be provided when deploying the web app."
  }
}

variable "app_service_log_analytics_workspace_id" {
  description = "Optional Log Analytics workspace resource ID for App Service diagnostics."
  type        = string
  default     = null
}

variable "app_service_app_settings" {
  description = "Additional application settings applied to the primary web app."
  type        = map(string)
  default     = {}
}

variable "app_service_connection_strings" {
  description = "Connection strings exposed to the primary web application."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "enable_arbitration_app_service" {
  description = "Toggle deployment of the arbitration App Service."
  type        = bool
  default     = false
}

variable "arbitration_app_plan_sku" {
  description = "Optional SKU override for the arbitration App Service plan."
  type        = string
  default     = null
}

variable "arbitration_runtime_stack" {
  description = "Runtime stack for the arbitration App Service."
  type        = string
  default     = "dotnet"
}

variable "arbitration_runtime_version" {
  description = "Runtime version for the arbitration App Service."
  type        = string
  default     = "8.0"
}

variable "arbitration_app_insights_connection_string" {
  description = "Optional Application Insights connection string for the arbitration app (defaults to the primary web app string)."
  type        = string
  default     = null

  validation {
    condition     = var.arbitration_app_insights_connection_string == null || trimspace(var.arbitration_app_insights_connection_string) != ""
    error_message = "arbitration_app_insights_connection_string cannot be blank; omit it to reuse the primary web app setting."
  }
}

variable "arbitration_log_analytics_workspace_id" {
  description = "Optional Log Analytics workspace resource ID dedicated to the arbitration app."
  type        = string
  default     = null
}

variable "arbitration_app_settings" {
  description = "Application settings applied to the arbitration App Service (expects Storage__Connection and Storage__Container entries)."
  type        = map(string)
  default     = {}
}

variable "arbitration_connection_strings" {
  description = "Connection strings exposed to the arbitration App Service (expects ConnStr and IDRConnStr entries sourced from Key Vault)."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "arbitration_run_from_package" {
  description = "Flag controlling WEBSITE_RUN_FROM_PACKAGE for the arbitration App Service."
  type        = bool
  default     = true
}
