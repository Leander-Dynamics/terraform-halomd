variable "server_name" {
  description = "Name of the SQL server."
  type        = string
}

variable "db_name" {
  description = "Name of the SQL database."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the SQL resources."
  type        = string
}

variable "location" {
  description = "Azure region for the SQL server."
  type        = string
}

variable "admin_login" {
  description = "Administrator login for the SQL server."
  type        = string
}

variable "admin_password" {
  description = "Administrator password for the SQL server."
  type        = string
  sensitive   = true
}

variable "minimum_tls_version" {
  description = "Minimum TLS version for the SQL server."
  type        = string
  default     = "1.2"
}

variable "public_network_access_enabled" {
  description = "Whether public network access is enabled on the SQL server."
  type        = bool
  default     = false
}

variable "sku_name" {
  description = "SKU name for the SQL database."
  type        = string
  default     = "GP_S_Gen5_2"
}

variable "max_size_gb" {
  description = "Maximum size of the SQL database in GB."
  type        = number
  default     = 32
}

variable "min_capacity" {
  description = "Minimum capacity for the SQL database (in vCores)."
  type        = number
  default     = 0.5
}

variable "auto_pause_delay_in_minutes" {
  description = "Auto-pause delay for the SQL database in minutes."
  type        = number
  default     = 60
}

variable "zone_redundant" {
  description = "Whether the database should be zone redundant."
  type        = bool
  default     = false
}

variable "backup_storage_redundancy" {
  description = "Backup storage redundancy for the database."
  type        = string
  default     = "Local"
}

variable "firewall_rules" {
  description = "Firewall rules to apply to the SQL server."
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
  default = []
}

variable "tags" {
  description = "Tags to apply to SQL resources."
  type        = map(string)
  default     = {}
}
