variable "name" {
  description = "Name of the SQL server."
  type        = string
}

variable "location" {
  description = "Azure region where the server will be deployed."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group that will contain the server."
  type        = string
}

variable "admin_login" {
  description = "Administrator login for the SQL server. Provide together with admin_password or leave null when configuring Azure AD administration."
  type        = string
  default     = null
}

variable "admin_password" {
  description = "Administrator password for the SQL server. Provide together with admin_login or leave null when configuring Azure AD administration."
  type        = string
  default     = null
  sensitive   = true
}

variable "azuread_administrator" {
  description = "Optional Azure AD administrator configuration for the SQL server."
  type = object({
    login_username = string
    object_id      = string
    tenant_id      = optional(string)
  })
  default = null

  validation {
    condition = var.azuread_administrator == null || (
      trimspace(var.azuread_administrator.login_username) != "" &&
      trimspace(var.azuread_administrator.object_id) != ""
    )
    error_message = "When specified, azuread_administrator.login_username and azuread_administrator.object_id must be non-empty."
  }
}
