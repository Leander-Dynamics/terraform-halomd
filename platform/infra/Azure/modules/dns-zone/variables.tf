variable "zone_name" {
  description = "DNS zone to manage."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the DNS zone."
  type        = string
}

variable "tags" {
  description = "Tags applied to the DNS zone."
  type        = map(string)
  default     = {}
}

variable "a_records" {
  description = "Map of DNS A records to create (key is the record name)."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "cname_records" {
  description = "Map of DNS CNAME records to create (key is the record name)."
  type = map(object({
    ttl   = number
    record = string
  }))
  default = {}
}
