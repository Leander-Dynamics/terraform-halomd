variable "name" {
  type = string
}

variable "storage_account_name" {
  description = "Name of the existing storage account that hosts the container."
  type        = string
}

variable "access_type" {
  type    = string
  default = "private"
}
