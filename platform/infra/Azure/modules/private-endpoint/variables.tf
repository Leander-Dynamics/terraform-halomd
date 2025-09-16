variable "name" {
  description = "Name of the private endpoint resource."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group where the private endpoint is created."
  type        = string
}

variable "location" {
  description = "Azure region for the private endpoint."
  type        = string
}

variable "subnet_id" {
  description = "Subnet resource ID hosting the private endpoint network interface."
  type        = string
}

variable "tags" {
  description = "Tags applied to the private endpoint resource."
  type        = map(string)
  default     = {}
}

variable "private_service_connection" {
  description = "Configuration for the private service connection bound to the endpoint."
  type = object({
    name                           = string
    private_connection_resource_id = string
    subresource_names              = optional(list(string), [])
    is_manual_connection           = optional(bool, false)
    request_message                = optional(string, null)
  })
}

variable "private_dns_zone_groups" {
  description = "Optional private DNS zone group associations for the endpoint."
  type = list(object({
    name                 = string
    private_dns_zone_ids = list(string)
  }))
  default = []
}
