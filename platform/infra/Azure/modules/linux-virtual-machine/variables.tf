variable "name" {}
variable "location" {}
variable "resource_group_name" {}
variable "sku" { default = "Standard" }
variable "nic_id" {
  default = ""
}
