variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "dns_prefix" {
  type    = string
  default = ""
}

variable "node_count" {
  type    = number
  default = 1
}

variable "vm_size" {
  type    = string
  default = "Standard_DS2_v2"
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace resource ID used for Container Insights."
  type        = string
  default     = null
}
