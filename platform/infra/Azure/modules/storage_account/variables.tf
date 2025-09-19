variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "location" {
  type        = string
  description = "Location for the storage account"
}

variable "name" {
  type        = string
  description = "Storage account name"
}

variable "account_tier" {
  type        = string
  description = "Storage account tier (Standard/Premium)"
}

variable "account_replication_type" {
  type        = string
  description = "Replication type (LRS, ZRS, GRS, etc.)"
}

variable "hns_enabled" {
  type        = string
  description = "make this true"
}

variable "sftp_enabled" {
  type        = string
  description = "Enable sftp support"
}

