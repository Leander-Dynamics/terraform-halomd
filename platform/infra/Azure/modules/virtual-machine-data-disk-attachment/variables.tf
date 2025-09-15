variable "disk_id" {
  description = "The resource ID of the managed disk to attach."
  type        = string
}

variable "vm_id" {
  description = "The resource ID of the virtual machine to which the disk will be attached."
  type        = string
}

variable "lun" {
  description = "Logical Unit Number (LUN) for the data disk. Must be unique per VM."
  type        = number
  default     = 0
}
