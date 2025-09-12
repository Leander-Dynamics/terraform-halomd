variable "name"                           { type = string }
variable "resource_group_name"            { type = string }
variable "location"                       { type = string }
variable "service_plan_id"               { type = string }
variable "runtime" {
  type    = string
  default = "dotnet"
}
variable "app_insights_connection_string" {
  type = string
}
variable "app_settings" {
  type    = map(string)
  default = {}
}
variable "tags" {
  type    = map(string)
  default = {}
}
