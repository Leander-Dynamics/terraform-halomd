variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "log_name"            { type = string }
variable "appi_name"           { type = string }
variable "tags" {
  type    = map(string)
  default = {}
}
