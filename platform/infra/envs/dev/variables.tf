# variables.tf - Cleaned and safe structure

variable "location" {
  description = "Azure region"
  type        = string
}

variable "env_name" {
  description = "Environment name (dev, stage, prod)"
  type        = string
}

variable "project_name" {
  description = "Project prefix (e.g. arbit)"
  type        = string
}

variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "tenant_id" {
  description = "Azure Tenant ID"
  type        = string
}

variable "app_insights_name" {
  type        = string
  description = "App Insights name"
}

variable "app_insights_resource_group_name" {
  type        = string
  description = "Optional resource group name for monitoring resources"
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags to apply to resources"
  default     = {}
}

variable "app_gateway_subnet_id" {
  description = "Subnet resource ID for the application gateway."
  type        = string
}

variable "app_gateway_backend_hostnames" {
  description = "List of backend hostnames for the application gateway."
  type        = list(string)
}

variable "dns_zone_name" {
  description = "Public DNS zone name to manage."
  type        = string
  default     = "az.halomd.com"
}

variable "dns_a_records" {
  description = "DNS A records to create (keyed by record name)."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "dns_cname_records" {
  description = "DNS CNAME records to create (keyed by record name)."
  type = map(object({
    ttl   = number
    record = string
  }))
  default = {}
}

variable "enable_aks" {
  type        = bool
  description = "Enable AKS deployment"
  default     = false
}

variable "enable_acr" {
  type        = bool
  description = "Enable ACR deployment"
  default     = false
}

variable "enable_storage" {
  type        = bool
  description = "Enable Storage Account deployment"
  default     = false
}

variable "enable_sql" {
  type        = bool
  description = "Enable SQL deployment"
  default     = false
}

variable "kv_public_network_access" {
  type        = bool
  description = "Allow public network access to Key Vault"
  default     = true
}

variable "acr_sku" {
  type        = string
  description = "SKU for Azure Container Registry"
  default     = "Basic"
}

variable "aks_node_count" {
  type        = number
  description = "Number of nodes for AKS"
  default     = 1
}

variable "aks_vm_size" {
  type        = string
  description = "VM size for AKS nodes"
  default     = "Standard_DS2_v2"
}

variable "web_plan_sku" {
  type        = string
  description = "App Service plan SKU"
  default     = "B1"
}

variable "func_plan_sku" {
  type        = string
  description = "Function App plan SKU"
  default     = "Y1"
}

variable "web_dotnet_version" {
  type        = string
  description = ".NET version for Web App"
  default     = "8.0"
}

variable "arbitration_plan_sku" {
  type        = string
  description = "App Service plan SKU for arbitration app"
  default     = "B1"
}

variable "arbitration_runtime_stack" {
  type        = string
  description = "Runtime stack for the arbitration web app"
  default     = "dotnet"
}

variable "arbitration_runtime_version" {
  type        = string
  description = "Runtime version for the arbitration web app"
  default     = "8.0"
}

variable "arbitration_connection_strings" {
  description = "Connection strings to configure on the arbitration app"
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "arbitration_app_settings" {
  description = "Additional app settings for the arbitration app"
  type        = map(string)
  default     = {}
}

variable "arbitration_run_from_package" {
  description = "Whether the arbitration app should run from package"
  type        = bool
  default     = true
}

variable "function_external_runtime" {
  type        = string
  description = "Runtime for external function app"
  default     = "dotnet"
}

variable "function_cron_runtime" {
  type        = string
  description = "Runtime for cron function app"
  default     = "python"
}

variable "sql_db_name" {
  type        = string
  description = "SQL database name"
  default     = "halomd"
}

variable "sql_sku_name" {
  type        = string
  description = "SQL database SKU"
  default     = "GP_S_Gen5_2"
}

variable "sql_auto_pause_minutes" {
  type        = number
  description = "Auto pause delay in minutes"
  default     = 60
}

variable "sql_max_size_gb" {
  type        = number
  description = "Maximum size of the SQL database in GB"
  default     = 75
}

variable "sql_public_network_access" {
  type        = bool
  description = "Allow public network access to SQL"
  default     = true
}

variable "sql_firewall_rules" {
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
}

variable "sql_admin_login" {
  type        = string
  description = "SQL administrator login"
  default     = ""
}

variable "sql_admin_password" {
  type        = string
  description = "SQL administrator password"
  default     = ""
  sensitive   = true
}

variable "sql_firewall_rules" {
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
}
