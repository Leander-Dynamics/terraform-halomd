variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "public_network_access_enabled" {
  type    = bool
  default = true
}

variable "enable_rbac_authorization" {
  type    = bool
  default = true
}

variable "secrets" {
  description = "Map of secrets to populate in the Key Vault."
  type = map(object({
    value        = string
    content_type = optional(string)
    tags         = optional(map(string))
  }))
  default   = {}
  sensitive = true
}

variable "rbac_assignments" {
  description = "Map of RBAC assignments to create for the Key Vault scope."
  type = map(object({
    principal_id         = string
    role_definition_id   = optional(string)
    role_definition_name = optional(string)
  }))
  default = {}
}
