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
  type    = bool
  default = false
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
