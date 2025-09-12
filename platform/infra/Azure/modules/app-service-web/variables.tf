variable "name"                           { type = string }
variable "plan_name"                      { type = string }
variable "resource_group_name"            { type = string }
variable "location"                       { type = string }
variable "plan_sku" {
  type    = string
  default = "B1"
}
variable "dotnet_version" {
  type    = string
  default = "8.0"
}
variable "app_insights_connection_string" { type = string }
variable "app_settings" {
  type    = map(string)
  default = {}
}

variable "tags" {
  type    = map(string)
  default = {}
}
