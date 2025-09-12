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
  type    = string
  default = ""
}
variable "admin_password" {
  type      = string
  default   = ""
  sensitive = true
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
