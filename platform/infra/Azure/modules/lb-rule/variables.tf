variable "name" {}
variable "resource_group_name" {}
variable "loadbalancer_id" {}
variable "protocol" { default = "Tcp" }
variable "frontend_port" {}
variable "backend_port" {}
variable "frontend_ip_name" {}
variable "backend_pool_id" {}
variable "probe_id" {}
