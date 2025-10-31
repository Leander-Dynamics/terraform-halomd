variable "resource_group_name" {
  description = "Resource group to place Front Door profile"
  type        = string
}

variable "profile_name" {
  description = "Front Door profile name"
  type        = string
}

variable "endpoint_name" {
  description = "Front Door endpoint name"
  type        = string
}

variable "origin_host_name" {
  description = "Origin hostname (e.g., public IP DNS of AKS ingress)"
  type        = string
}

variable "tags" {
  description = "Tags to apply"
  type        = map(string)
  default     = {}
}
