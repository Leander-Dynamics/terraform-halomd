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

variable "sql_admin_password_secret_name" {
  description = "Name of the Key Vault secret that stores the SQL administrator password."
  type        = string
  default     = null
}

# -------------------------
# Arbitration
# -------------------------
variable "arbitration_plan_sku" {
  description = "SKU for the arbitration App Service plan."
  type        = string
  default     = ""
}

variable "arbitration_runtime_stack" {
  description = "Runtime stack used by the arbitration App Service."
  type        = string
  default     = ""
}

variable "arbitration_runtime_version" {
  description = "Runtime version for the arbitration App Service."
  type        = string
  default     = ""
}

variable "arbitration_app_settings" {
  description = "App settings applied to the arbitration App Service."
  type        = map(string)
  default     = {}
}

variable "arbitration_connection_strings" {
  description = "Connection strings for the arbitration App Service."
  type = list(object({
    name  = string
    type  = string
    value = string
  }))
  default = []
}
