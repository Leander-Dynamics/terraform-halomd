variable "name" {
  type = string
}

variable "server_id" {
  type = string
}

variable "sku_name" {
  type = string
}

variable "tags" {
  description = "Optional tags to apply to the SQL database."
  type        = map(string)
  default     = {}
}
