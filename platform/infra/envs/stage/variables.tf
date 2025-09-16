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
