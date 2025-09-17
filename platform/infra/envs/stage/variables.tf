variable "sql_admin_login" {
  description = "Administrator login for the SQL server."
  type        = string
  default     = null  # ✅ Use null so secrets can be injected securely via Key Vault or CI/CD variables
}

variable "sql_admin_password" {
  description = "Administrator password for the SQL server."
  type        = string
  sensitive   = true
  default     = null  # ✅ Use null and mark as sensitive for security
}
