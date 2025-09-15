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

variable "app_insights_connection_string" {
  type        = string
  description = "App Insights connection string"
}

variable "app_insights_name" {
  type        = string
  description = "App Insights name"
}

variable "app_insights_rg" {
  type        = string
  description = "App Insights Resource Group name"
}

variable "tags" {
  type        = map(string)
  description = "Tags to apply to resources"
  default     = {}
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
  default     = 32
}

variable "sql_public_network_access" {
  type        = bool
  description = "Allow public network access to SQL"
  default     = true
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
