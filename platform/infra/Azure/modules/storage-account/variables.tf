variable "name"                { type = string }
variable "resource_group_name" { type = string }
variable "location"            { type = string }
variable "account_tier" {
  type    = string
  default = "Standard"
}
variable "replication_type" {
  type    = string
  default = "LRS"
}

variable "allow_blob_public_access" {
  description = "When true, anonymous clients can read container and blob data; false disables all anonymous access."
  type        = bool
  default     = false
}

variable "min_tls_version" {
  type    = string
  default = "TLS1_2"
}

variable "enable_hns" {
  type    = bool
  default = false
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "network_rules" {
  description = "Optional network rules applied to the storage account."
  type = object({
    bypass                     = optional(list(string))
    default_action             = optional(string)
    ip_rules                   = optional(list(string))
    virtual_network_subnet_ids = optional(list(string))
  })
  default = null
}

variable "private_endpoints" {
  description = "Private endpoint definitions associated with the storage account for network rule augmentation."
  type = list(object({
    id        = optional(string)
    subnet_id = optional(string)
  }))
  default = []
}
