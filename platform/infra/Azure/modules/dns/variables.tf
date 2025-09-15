variable "zone_name" {
  description = "Name of the DNS zone."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group where the DNS zone exists."
  type        = string
}

variable "tags" {
  description = "Tags to apply to the DNS zone."
  type        = map(string)
  default     = {}
}

variable "a_records" {
  description = "Map of DNS A records to create."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "cname_records" {
  description = "Map of DNS CNAME records to create."
  type = map(object({
    ttl   = number
    record = string
  }))
  default = {}
}
