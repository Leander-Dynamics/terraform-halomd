variable "server_name"                 { type = string }
variable "db_name" {
  type    = string
  default = "halomd"
}
variable "resource_group_name" {
  type = string
}
variable "location" {
  type = string
}
variable "admin_login" {
  description = "Administrator login for the SQL server."
  type        = string

  validation {
    condition     = length(trimspace(var.admin_login)) > 0
    error_message = "Administrator login must be provided."
  }
}
variable "admin_password" {
  description = "Administrator password for the SQL server."
  type        = string
  sensitive   = true

  validation {
    condition     = length(trimspace(var.admin_password)) > 0
    error_message = "Administrator password must be provided."
  }
}
variable "public_network_access_enabled" {
  type    = bool
  default = true
}
variable "minimum_tls_version" {
  type    = string
  default = "1.2"
}
variable "sku_name" {
  type    = string
  default = "GP_S_Gen5_2"
}
variable "auto_pause_delay_in_minutes" {
  type    = number
  default = 60
}
variable "max_size_gb" {
  type    = number
  default = 32
}
variable "tags" {
  type    = map(string)
  default = {}
}
variable "firewall_rules" {
  type = list(object({ name = string, start_ip = string, end_ip = string }))
  default = []
}
