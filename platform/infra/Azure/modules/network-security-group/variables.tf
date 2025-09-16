variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "tags" {
  description = "Optional tags to apply to the network security group."
  type        = map(string)
  default     = {}
}
