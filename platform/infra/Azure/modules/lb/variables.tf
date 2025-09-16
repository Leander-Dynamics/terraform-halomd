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
  type    = string
  default = "Standard"

  validation {
    condition     = contains(["Standard", "Basic"], var.sku)
    error_message = "The sku must be either \"Standard\" or \"Basic\"."
  }
}

variable "public_ip_id" {
  type = string
}

variable "tags" {
  description = "Optional tags to apply to the load balancer."
  type        = map(string)
  default     = {}
}
