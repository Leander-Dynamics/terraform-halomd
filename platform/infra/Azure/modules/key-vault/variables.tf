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

variable "network_acls" {
  description = "Optional network ACL configuration applied to the Key Vault."
  type = object({
    bypass                     = optional(string)
    default_action             = optional(string)
    ip_rules                   = optional(list(string))
    virtual_network_subnet_ids = optional(list(string))
  })
  default = null
}

variable "private_endpoints" {
  description = "Private endpoint definitions associated with the Key Vault for network ACL augmentation."
  type = list(object({
    id        = optional(string)
    subnet_id = optional(string)
  }))
  default = []
}
