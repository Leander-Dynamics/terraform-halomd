variable "name"                           { type = string }
variable "plan_name"                      { type = string }
variable "resource_group_name"            { type = string }
variable "location"                       { type = string }
variable "plan_sku" {
  type    = string
  default = "Y1"
}
variable "runtime" {
  type    = string
  default = "dotnet"
}
variable "app_insights_connection_string" {
  type = string
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace resource ID for diagnostic settings."
  type        = string
  default     = null
}
variable "app_settings" {
  type    = map(string)
  default = {}
}
variable "tags" {
  type    = map(string)
  default = {}
}
