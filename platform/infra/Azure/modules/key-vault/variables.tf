variable "name"                { type = string }
variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "tags" {
  type    = map(string)
  default = {}
}

variable "public_network_access_enabled" {
  type    = bool
  default = true
}

variable "secrets" {
  description = "Map of secrets to create within the Key Vault."
  type        = map(string)
  default     = {}
}
