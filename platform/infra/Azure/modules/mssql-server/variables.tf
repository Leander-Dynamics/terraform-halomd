variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "administrator_login" {
  type = string
}

variable "administrator_password" {
  type      = string
  sensitive = true
}

variable "sku" {
  default = "Standard"
}

variable "tags" {
  description = "Optional tags to apply to the SQL server."
  type        = map(string)
  default     = {}
}
