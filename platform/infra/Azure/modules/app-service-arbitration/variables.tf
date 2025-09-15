variable "name"                { type = string }
variable "plan_name"           { type = string }
variable "resource_group_name" { type = string }
variable "location"            { type = string }

variable "plan_sku" {
  type    = string
  default = "B1"
}

variable "runtime_stack" {
  type    = string
  default = "dotnet"
}

variable "runtime_version" {
  type    = string
  default = "8.0"
}

variable "app_insights_connection_string" { type = string }

variable "connection_strings" {
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "app_settings" {
  type    = map(string)
  default = {}
}

variable "run_from_package" {
  type    = bool
  default = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
