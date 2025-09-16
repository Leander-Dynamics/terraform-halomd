variable "name" {
  description = "Name of the App Service."
  type        = string
}

variable "location" {
  description = "Azure region where the App Service will be deployed."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group in which the App Service will be created."
  type        = string
}

variable "service_plan_id" {
  description = "Resource ID of the App Service Plan that hosts the App Service."
  type        = string
  default     = ""
}

variable "tags" {
  description = "Optional tags to apply to the Linux Web App."
  type        = map(string)
  default     = {}
}
