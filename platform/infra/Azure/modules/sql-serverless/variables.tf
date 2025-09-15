variable "server_name" {
  description = "Name of the SQL Server."
  type        = string
}

variable "database_name" {
  description = "Name of the SQL Database."
  type        = string
  default     = "halomd"
}

variable "resource_group_name" {
  description = "Resource group hosting the SQL Server."
  type        = string
}

variable "location" {
  description = "Azure region where the SQL Server is deployed."
  type        = string
}

variable "administrator_login" {
  description = "Administrator login for the SQL Server."
  type        = string
}

variable "administrator_password" {
  description = "Administrator password for the SQL Server."
  type        = string
  sensitive   = true
}

variable "public_network_access_enabled" {
  description = "Whether public network access is allowed."
  type        = bool
  default     = true
}

variable "minimum_tls_version" {
  description = "Minimum TLS version allowed."
  type        = string
  default     = "1.2"
}

variable "sku_name" {
  description = "SKU for the serverless database (e.g., GP_S_Gen5_2)."
  type        = string
  default     = "GP_S_Gen5_2"
}

variable "max_size_gb" {
  description = "Maximum database size in GB."
  type        = number
  default     = 75
}

variable "auto_pause_delay_in_minutes" {
  description = "Auto-pause delay (in minutes) for the serverless database."
  type        = number
  default     = 60
}

variable "min_capacity" {
  description = "Minimum vCore capacity."
  type        = number
  default     = 0.5
}

variable "max_capacity" {
  description = "Maximum vCore capacity."
  type        = number
  default     = 4
}

variable "read_scale" {
  description = "Enable read scale-out."
  type        = bool
  default     = false
}

variable "zone_redundant" {
  description = "Enable zone redundancy for the database."
  type        = bool
  default     = false
}

variable "collation" {
  description = "Database collation."
  type        = string
  default     = "SQL_Latin1_General_CP1_CI_AS"
}

variable "backup_storage_redundancy" {
  description = "Specifies the backup storage redundancy for the database. Possible values: Local, Zone, Geo."
  type        = string
  default     = "Local"
}

variable "firewall_rules" {
  description = "List of firewall rules applied to the SQL Server."
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
