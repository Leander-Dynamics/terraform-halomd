
# variables.tf - Cleaned and safe structure

variable "location" {
  description = "Azure region"
  type        = string
}

variable "env_name" {
  description = "Environment name (dev, stage, prod)"
  type        = string
}

variable "project_name" {
  description = "Project prefix"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "storage_account_name" {
  description = "Storage account for backend state"
  type        = string
}

variable "container_name" {
  description = "Container for backend state"
  type        = string
}

variable "key" {
  description = "Key (file name) of the backend state"
  type        = string
}

variable "use_azuread_auth" {
  description = "Whether to use Azure AD authentication for the backend"
  type        = bool
  default     = true
}
variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "tenant_id" {
  description = "Azure Tenant ID"
  type        = string
}

variable "sql_firewall_rules" {
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
}

variable "dns_zone_name" {
  description = "Public DNS zone name to manage."
  type        = string
  default     = "az.halomd.com"
}

variable "dns_a_records" {
  description = "DNS A records to create (keyed by record name)."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "dns_cname_records" {
  description = "DNS CNAME records to create (keyed by record name)."
  type = map(object({
    ttl   = number
    record = string
  }))
  default = {}
}
