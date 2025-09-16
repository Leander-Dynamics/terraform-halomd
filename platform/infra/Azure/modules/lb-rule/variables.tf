variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "loadbalancer_id" {
  type = string
}

variable "protocol" {
  type    = string
  default = "Tcp"
}

variable "frontend_port" {
  type = number
}

variable "backend_port" {
  type = number
}

variable "frontend_ip_name" {
  type = string
}

variable "backend_pool_id" {
  type = string
}

variable "probe_id" {
  type = string
}
