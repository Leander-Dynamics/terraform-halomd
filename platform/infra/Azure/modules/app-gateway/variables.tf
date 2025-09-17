variable "name" {
  description = "Name of the Application Gateway."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group in which to deploy the Application Gateway."
  type        = string
}

variable "location" {
  description = "Azure location for the Application Gateway."
  type        = string
}

variable "subnet_id" {
  description = "Resource ID of the subnet where the Application Gateway will be placed."
  type        = string
}

variable "fqdn_prefix" {
  description = "Domain name label used for the public IP."
  type        = string
}

variable "backend_fqdns" {
  description = "List of backend target hostnames."
  type        = list(string)

  validation {
    condition     = length(var.backend_fqdns) > 0
    error_message = "At least one backend hostname must be provided for the Application Gateway."
  }
}

variable "backend_port" {
  description = "Port used to communicate with backend targets."
  type        = number
  default     = 443
}

variable "frontend_port" {
  description = "Port that the frontend listener will listen on."
  type        = number
  default     = 80
}

variable "frontend_protocol" {
  description = "Protocol used by the frontend listener."
  type        = string
  default     = "Http"

  validation {
    condition     = contains(["Http", "Https"], var.frontend_protocol)
    error_message = "The frontend protocol must be either 'Http' or 'Https'."
  }
}

variable "listener_protocol" {
  description = "Protocol for the default listener (legacy compatibility)."
  type        = string
  default     = "Http"
}

variable "ssl_certificate" {
  description = "Optional SSL certificate settings when using an HTTPS frontend listener."
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

variable "request_timeout" {
  description = "Timeout, in seconds, for requests to the backend."
  type        = number
  default     = 30
}

variable "health_probe_path" {
  description = "Path used by the health probe."
  type        = string
  default     = "/"
}

variable "health_probe_interval" {
  description = "Interval, in seconds, between health probe requests."
  type        = number
  default     = 30
}

variable "health_probe_timeout" {
  description = "Timeout, in seconds, for the health probe."
  type        = number
  default     = 30
}

variable "health_probe_unhealthy_threshold" {
  description = "Number of failed probes before a backend is considered unhealthy."
  type        = number
  default     = 3
}

variable "pick_host_name_from_backend_address" {
  description = "Whether to use the backend address as the host header."
  type        = bool
  default     = true
}

variable "sku_name" {
  description = "SKU name for the Application Gateway."
  type        = string
  default     = "Standard_v2"
}

variable "sku_tier" {
  description = "SKU tier for the Application Gateway."
  type        = string
  default     = "Standard_v2"
}

variable "sku_capacity" {
  description = "Number of instances to run for the Application Gateway."
  type        = number
  default     = 1
}

variable "enable_http2" {
  description = "Whether to enable HTTP/2 on the Application Gateway."
  type        = bool
  default     = true
}

variable "tags" {
  description = "Tags to apply to all resources created by the module."
  type        = map(string)
  default     = {}
}
variable "backend_protocol" {
  type    = string
  default = "Https"
}

variable "backend_request_timeout" {
  type    = number
  default = 20
}