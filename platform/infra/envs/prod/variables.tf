variable "project_name" {
  description = "Project or application name used as part of resource naming."
  type        = string
}

variable "env_name" {
  description = "Short environment name (e.g. dev, stage, prod)."
  type        = string
}

variable "location" {
  description = "Azure region for the deployment."
  type        = string
}

variable "subscription_id" {
  description = "Azure subscription ID used by the provider."
  type        = string
}

variable "tenant_id" {
  description = "Azure AD tenant ID used by the provider."
  type        = string
}

variable "tags" {
  description = "Base tags applied to all resources."
  type        = map(string)
  default     = {}
}

variable "resource_group_name" {
  description = "Override for the resource group name. Leave null to use the default convention."
  type        = string
  default     = null
}

variable "network_enabled" {
  description = "Toggle to create the virtual network module."
  type        = bool
  default     = false
}

variable "network_name" {
  description = "Optional override for the virtual network name."
  type        = string
  default     = null
}

variable "network_address_space" {
  description = "Address space for the virtual network."
  type        = list(string)
  default     = []
}

variable "network_subnets" {
  description = "Map of subnets to create when the network module is enabled."
  type = map(object({
    address_prefixes  = list(string)
    service_endpoints = optional(list(string), [])
  }))
  default = {}
}

variable "dns_enabled" {
  description = "Toggle to manage a DNS zone."
  type        = bool
  default     = false
}

variable "dns_zone_name" {
  description = "DNS zone name to manage when enabled."
  type        = string
  default     = null
}

variable "dns_a_records" {
  description = "A records to create in the DNS zone."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "dns_cname_records" {
  description = "CNAME records to create in the DNS zone."
  type = map(object({
    ttl   = number
    record = string
  }))
  default = {}
}

variable "app_service_enabled" {
  description = "Toggle to provision the App Service module."
  type        = bool
  default     = false
}

variable "app_service_name" {
  description = "Optional override for the App Service name."
  type        = string
  default     = null
}

variable "app_service_plan_name" {
  description = "Optional override for the App Service plan name."
  type        = string
  default     = null
}

variable "app_service_plan_sku" {
  description = "SKU for the App Service plan."
  type        = string
  default     = "B1"
}

variable "app_service_dotnet_version" {
  description = ".NET version used by the App Service."
  type        = string
  default     = "8.0"
}

variable "app_service_app_settings" {
  description = "Additional App Service app settings."
  type        = map(string)
  default     = {}
}

variable "app_service_connection_strings" {
  description = "Connection strings for the App Service."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "app_service_app_insights_connection_string" {
  description = "Application Insights connection string injected into the App Service."
  type        = string
  default     = null
}

variable "app_service_log_analytics_workspace_id" {
  description = "Log Analytics workspace ID for diagnostic settings."
  type        = string
  default     = null
}

variable "app_service_https_only" {
  description = "Force HTTPS-only access to the App Service."
  type        = bool
  default     = true
}

variable "app_service_always_on" {
  description = "Enable Always On for the App Service."
  type        = bool
  default     = true
}

variable "app_service_identity_type" {
  description = "Managed identity type for the App Service."
  type        = string
  default     = "SystemAssigned"
}

variable "sql_enabled" {
  description = "Toggle to provision the SQL module."
  type        = bool
  default     = false
}

variable "sql_server_name" {
  description = "Optional override for the SQL server name."
  type        = string
  default     = null
}

variable "sql_database_name" {
  description = "Optional override for the SQL database name."
  type        = string
  default     = null
}

variable "sql_admin_login" {
  description = "Administrator login for the SQL server."
  type        = string
  default     = ""
}

variable "sql_admin_password" {
  description = "Administrator password for the SQL server."
  type        = string
  sensitive   = true
  default     = ""
}

variable "sql_minimum_tls_version" {
  description = "Minimum TLS version enforced on the SQL server."
  type        = string
  default     = "1.2"
}

variable "sql_public_network_access_enabled" {
  description = "Allow public network access to the SQL server."
  type        = bool
  default     = false
}

variable "sql_sku_name" {
  description = "SKU for the SQL database."
  type        = string
  default     = "GP_S_Gen5_2"
}

variable "sql_max_size_gb" {
  description = "Maximum size of the SQL database in GB."
  type        = number
  default     = 32
}

variable "sql_min_capacity" {
  description = "Minimum capacity for the SQL database in vCores."
  type        = number
  default     = 0.5
}

variable "sql_auto_pause_delay_in_minutes" {
  description = "Auto pause delay for the SQL database in minutes."
  type        = number
  default     = 60
}

variable "sql_zone_redundant" {
  description = "Enable zone redundancy for the SQL database."
  type        = bool
  default     = false
}

variable "sql_backup_storage_redundancy" {
  description = "Backup storage redundancy for the SQL database."
  type        = string
  default     = "Local"
}

variable "sql_firewall_rules" {
  description = "Firewall rules applied to the SQL server."
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
  default = []
}
