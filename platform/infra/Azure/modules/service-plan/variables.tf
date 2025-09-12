variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "sku" {
  type = string
}

variable "os_type" {
  type    = string
  default = "Linux"
}

variable "tags" {
  type    = map(string)
  default = {}
}
