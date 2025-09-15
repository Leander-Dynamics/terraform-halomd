variable "name" {
  description = "Name of the Application Gateway."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group hosting the Application Gateway."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "subnet_id" {
  description = "Subnet ID where the Application Gateway is deployed."
  type        = string
}

variable "fqdn_prefix" {
  description = "Domain name label used for the public IP."
  type        = string
}

variable "backend_fqdns" {
  description = "List of backend FQDNs associated with the default pool."
  type        = list(string)
}

variable "backend_port" {
  description = "Backend port for HTTP settings."
  type        = number
  default     = 80
}

variable "backend_protocol" {
  description = "Backend protocol for HTTP settings."
  type        = string
  default     = "Http"
}

variable "backend_request_timeout" {
  description = "Request timeout for backend HTTP settings."
  type        = number
  default     = 30
}

variable "pick_host_name_from_backend_address" {
  description = "Whether to use the backend address as the host header."
  type        = bool
  default     = true
}

variable "frontend_port" {
  description = "Frontend port exposed by the Application Gateway."
  type        = number
  default     = 80
}

variable "listener_protocol" {
  description = "Protocol for the default listener."
  type        = string
  default     = "Http"
}

variable "sku_name" {
  description = "Application Gateway SKU name."
  type        = string
  default     = "Standard_v2"
}

variable "sku_tier" {
  description = "Application Gateway SKU tier."
  type        = string
  default     = "Standard_v2"
}

variable "sku_capacity" {
  description = "Number of capacity units."
  type        = number
  default     = 1
}

variable "enable_http2" {
  description = "Enable HTTP/2 support."
  type        = bool
  default     = true
}

variable "ssl_certificate" {
  description = "Optional PFX certificate for HTTPS listeners."
  type = object({
    name     = string
    data     = string
    password = string
  })
  default = null
}

variable "trusted_client_certificates" {
  description = "Optional trusted client certificates for mutual TLS."
  type = list(object({
    name = string
    data = string
  }))
  default = []
}

variable "tags" {
  description = "Tags applied to the Application Gateway resources."
  type        = map(string)
  default     = {}
}
