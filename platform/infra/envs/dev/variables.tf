variable "location" {
  description = "Azure region for resource deployment."
  type        = string
}

variable "env_name" {
  description = "Environment name (e.g. dev, stage, prod)."
  type        = string
}

variable "project_name" {
  description = "Project or application identifier used for naming."
  type        = string
}

variable "subscription_id" {
  description = "Azure subscription ID."
  type        = string
  default     = null
}

variable "tenant_id" {
  description = "Azure tenant ID."
  type        = string
  default     = null
}

variable "tags" {
  description = "Common tags applied to all resources."
  type        = map(string)
  default     = {}
}

# -------------------------
# Feature toggles
# -------------------------
variable "enable_acr" {
  description = "Flag to enable Azure Container Registry provisioning."
  type        = bool
  default     = false
}

variable "enable_sql" {
  description = "Flag to deploy the SQL Serverless resources."
  type        = bool
  default     = false
}

variable "enable_nat_gateway" {
  description = "Flag to deploy a NAT Gateway for outbound connectivity."
  type        = bool
  default     = false

  validation {
    condition     = var.enable_nat_gateway == false || var.nat_gateway_configuration != null
    error_message = "nat_gateway_configuration must be provided when enable_nat_gateway is true."
  }
}

variable "enable_vpn_gateway" {
  description = "Flag to deploy a virtual network gateway for hybrid connectivity."
  type        = bool
  default     = false

  validation {
    condition     = var.enable_vpn_gateway == false || var.vpn_gateway_configuration != null
    error_message = "vpn_gateway_configuration must be provided when enable_vpn_gateway is true."
  }
}

variable "enable_bastion" {
  description = "Flag to deploy an Azure Bastion host."
  type        = bool
  default     = false
}

variable "kv_public_network_access" {
  description = "Allow public network access to the Key Vault."
  type        = bool
  default     = true
}

variable "kv_network_acls" {
  description = "Network ACL configuration applied to the Key Vault."
  type = object({
    bypass                     = optional(string)
    default_action             = optional(string)
    ip_rules                   = optional(list(string))
    virtual_network_subnet_ids = optional(list(string))
  })
  default = null
}

variable "enable_kv_private_endpoint" {
  description = "Toggle creation of a private endpoint for the Key Vault."
  type        = bool
  default     = false
}

variable "kv_private_endpoint_subnet_key" {
  description = "Key of the subnet used when creating the Key Vault private endpoint."
  type        = string
  default     = null
}

variable "kv_private_dns_zone_ids" {
  description = "Private DNS zone IDs linked to the Key Vault private endpoint."
  type        = list(string)
  default     = []
}

variable "kv_private_endpoint_resource_id" {
  description = "Optional override for the Key Vault resource ID used by the private endpoint module."
  type        = string
  default     = null
}

variable "storage_network_rules" {
  description = "Network rules applied to the storage account resource."
  type = object({
    bypass                     = optional(list(string))
    default_action             = optional(string)
    ip_rules                   = optional(list(string))
    virtual_network_subnet_ids = optional(list(string))
  })
  default = null
}

variable "enable_storage_private_endpoint" {
  description = "Toggle creation of a private endpoint for the storage account."
  type        = bool
  default     = false
}

variable "storage_private_endpoint_subnet_key" {
  description = "Key of the subnet used when creating the storage account private endpoint."
  type        = string
  default     = null
}

variable "storage_private_dns_zone_ids" {
  description = "Private DNS zone IDs linked to the storage account private endpoint."
  type        = list(string)
  default     = []
}

variable "storage_private_endpoint_subresource_names" {
  description = "Subresource names exposed through the storage account private endpoint."
  type        = list(string)
  default     = ["blob"]
}

variable "storage_account_private_connection_resource_id" {
  description = "Resource ID used by the storage account private endpoint connection."
  type        = string
  default     = null
}

# -------------------------
# Networking
# -------------------------
variable "vnet_address_space" {
  description = "Address space assigned to the virtual network."
  type        = list(string)
}

variable "vnet_dns_servers" {
  description = "Optional custom DNS servers applied to the virtual network."
  type        = list(string)
  default     = []
}

variable "subnets" {
  description = "Map of subnet definitions keyed by subnet name."
  type = map(object({
    address_prefixes  = list(string)
    service_endpoints = optional(list(string), [])
    delegations = optional(list(object({
      name = string
      service_delegation = object({
        name    = string
        actions = list(string)
      })
    })), [])
  }))
}

variable "nat_gateway_configuration" {
  description = "Configuration for the NAT Gateway when enabled."
  type = object({
    name                     = string
    subnet_keys              = list(string)
    public_ip_configurations = optional(list(object({
      name              = string
      allocation_method = optional(string, "Static")
      sku               = optional(string, "Standard")
      zones             = optional(list(string), [])
      tags              = optional(map(string), {})
    })), [])
    public_ip_ids           = optional(list(string), [])
    sku_name                = optional(string, "Standard")
    idle_timeout_in_minutes = optional(number, 4)
    zones                   = optional(list(string), [])
    tags                    = optional(map(string), {})
  })
  default = null

  validation {
    condition = var.nat_gateway_configuration == null ? true : (
      length(try(var.nat_gateway_configuration.public_ip_configurations, [])) +
      length(try(var.nat_gateway_configuration.public_ip_ids, []))
    ) > 0
    error_message = "At least one public IP configuration or existing public IP ID must be provided for the NAT Gateway."
  }

  validation {
    condition     = var.nat_gateway_configuration == null ? true : length(var.nat_gateway_configuration.subnet_keys) > 0
    error_message = "At least one subnet key must be provided to associate the NAT Gateway."
  }
}

variable "vpn_gateway_configuration" {
  description = "Configuration for the virtual network gateway when enabled."
  type = object({
    name                 = string
    gateway_subnet_key   = string
    sku                  = string
    gateway_type         = optional(string, "Vpn")
    vpn_type             = optional(string, "RouteBased")
    active_active        = optional(bool, false)
    enable_bgp           = optional(bool, false)
    generation           = optional(string)
    ip_configuration_name = optional(string, "default")
    custom_routes        = optional(list(string), [])
    public_ip = optional(object({
      name              = string
      allocation_method = optional(string, "Static")
      sku               = optional(string, "Standard")
      zones             = optional(list(string), [])
      tags              = optional(map(string), {})
    }))
    public_ip_id            = optional(string)
    vpn_client_configuration = optional(object({
      address_space         = list(string)
      vpn_client_protocols  = optional(list(string), ["OpenVPN"])
      vpn_auth_types        = optional(list(string), [])
      aad_tenant            = optional(string)
      aad_audience          = optional(string)
      aad_issuer            = optional(string)
      radius_server_address = optional(string)
      radius_server_secret  = optional(string)
      root_certificates = optional(list(object({
        name             = string
        public_cert_data = string
      })), [])
      revoked_certificates = optional(list(object({
        name       = string
        thumbprint = string
      })), [])
    }))
    bgp_settings = optional(object({
      asn         = number
      peer_weight = optional(number)
      peering_addresses = optional(list(object({
        ip_configuration_name = string
        apipa_addresses       = list(string)
      })), [])
    }))
    tags = optional(map(string), {})
  })
  default = null

  validation {
    condition = var.vpn_gateway_configuration == null ? true : (
      (try(var.vpn_gateway_configuration.public_ip, null) != null ? 1 : 0) +
      (try(var.vpn_gateway_configuration.public_ip_id, null) != null && try(trim(var.vpn_gateway_configuration.public_ip_id), "") != "" ? 1 : 0)
    ) > 0
    error_message = "Either a public_ip definition or public_ip_id must be supplied for the virtual network gateway."
  }
}

variable "app_gateway_subnet_key" {
  description = "Key of the subnet used for the Application Gateway."
  type        = string
}

variable "bastion_subnet_key" {
  description = "Key of the subnet reserved for the Bastion host."
  type        = string
  default     = null
}

variable "app_gateway_subnet_id" {
  description = "Subnet resource ID for the Application Gateway."
  type        = string
}

# -------------------------
# Application Gateway
# -------------------------
variable "app_gateway_fqdn_prefix" {
  description = "Domain name label for the Application Gateway public IP."
  type        = string
}

variable "app_gateway_backend_fqdns" {
  description = "Additional backend FQDNs joined to the App Gateway pool."
  type        = list(string)
  default     = []
}

variable "app_gateway_backend_port" {
  description = "Backend port used by the Application Gateway."
  type        = number
  default     = 80
}

variable "app_gateway_backend_protocol" {
  description = "Backend protocol used by the Application Gateway."
  type        = string
  default     = "Http"
}

variable "app_gateway_frontend_port" {
  description = "Frontend port exposed by the Application Gateway."
  type        = number
  default     = 80
}

variable "app_gateway_listener_protocol" {
  description = "Protocol for the default listener."
  type        = string
  default     = "Http"
}

variable "app_gateway_sku_name" {
  description = "Application Gateway SKU name."
  type        = string
  default     = "Standard_v2"
}

variable "app_gateway_sku_tier" {
  description = "Application Gateway SKU tier."
  type        = string
  default     = "Standard_v2"
}

variable "app_gateway_capacity" {
  description = "Application Gateway capacity units."
  type        = number
  default     = 1
}

variable "app_gateway_enable_http2" {
  description = "Enable HTTP/2 on the Application Gateway listener."
  type        = bool
  default     = true
}

variable "app_gateway_backend_request_timeout" {
  description = "Request timeout for backend HTTP settings."
  type        = number
  default     = 30
}

variable "app_gateway_pick_host_name" {
  description = "Use backend address host names for the host header."
  type        = bool
  default     = true
}

# -------------------------
# App Service
# -------------------------
variable "app_service_plan_sku" {
  description = "SKU used for the App Service plan."
  type        = string
}

variable "app_service_plan_os_type" {
  description = "Operating system for the App Service plan."
  type        = string
  default     = "Windows"
}

variable "app_service_fqdn_prefix" {
  description = "Name of the App Service (also the default FQDN prefix)."
  type        = string
}

variable "app_service_https_only" {
  description = "Force HTTPS traffic only."
  type        = bool
  default     = true
}

variable "app_service_always_on" {
  description = "Keep the App Service always on."
  type        = bool
  default     = true
}

variable "app_service_app_settings" {
  description = "Application settings applied to the App Service."
  type        = map(string)
  default     = {}
}

variable "app_service_connection_strings" {
  description = "Connection strings applied to the App Service."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

# -------------------------
# Monitoring
# -------------------------
variable "app_insights_resource_group_name" {
  description = "Optional resource group where Application Insights resources are created."
  type        = string
  default     = null
}

variable "app_insights_name" {
  description = "Optional name override for the Application Insights resource."
  type        = string
  default     = ""
}

# -------------------------
# DNS
# -------------------------
variable "dns_zone_name" {
  description = "DNS zone managed within the environment."
  type        = string
  default     = "az.halomd.com"
}

variable "dns_a_records" {
  description = "DNS A records managed by Terraform."
  type = map(object({
    ttl     = number
    records = list(string)
  }))
  default = {}
}

variable "dns_cname_records" {
  description = "DNS CNAME records managed by Terraform."
  type = map(object({
    ttl   = number
    record = string
  }))
  default = {}
}

# -------------------------
# SQL
# -------------------------
variable "sql_database_name" {
  description = "Optional SQL database name override."
  type        = string
  default     = ""
}

variable "sql_sku_name" {
  description = "SKU for the serverless SQL database."
  type        = string
}

variable "sql_max_size_gb" {
  description = "Maximum size of the SQL database in GB."
  type        = number
  default     = 75
}

variable "sql_auto_pause_delay" {
  description = "Auto pause delay for the SQL database."
  type        = number
  default     = 60
}

variable "sql_min_capacity" {
  description = "Minimum vCore capacity for the SQL database."
  type        = number
  default     = 0.5
}

variable "sql_max_capacity" {
  description = "Maximum vCore capacity for the SQL database."
  type        = number
  default     = 4
}

variable "sql_read_scale" {
  description = "Enable read scale-out on the SQL database."
  type        = bool
  default     = false
}

variable "sql_zone_redundant" {
  description = "Enable zone redundancy on the SQL database."
  type        = bool
  default     = false
}

variable "sql_collation" {
  description = "Collation applied to the SQL database."
  type        = string
  default     = "SQL_Latin1_General_CP1_CI_AS"
}

variable "sql_minimum_tls_version" {
  description = "Minimum TLS version enforced on the SQL server."
  type        = string
  default     = "1.2"
}

variable "sql_public_network_access" {
  description = "Allow public network access to the SQL server."
  type        = bool
  default     = true
}

variable "sql_firewall_rules" {
  description = "Firewall rules applied to the SQL server."
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
  default = []
}

variable "sql_admin_login" {
  description = "Administrator login for the SQL server."
  type        = string
}

variable "sql_admin_password" {
  description = "Administrator password for the SQL server."
  type        = string
  sensitive   = true
}

# -------------------------
# Arbitration
# -------------------------
variable "arbitration_plan_sku" {
  description = "SKU for the arbitration App Service plan."
  type        = string
  default     = null
}

variable "arbitration_runtime_stack" {
  description = "Runtime stack used by the arbitration App Service."
  type        = string
  default     = null
}

variable "arbitration_runtime_version" {
  description = "Runtime version for the arbitration App Service."
  type        = string
  default     = null
}

variable "arbitration_connection_strings" {
  description = "Connection strings applied to the arbitration App Service."
  type = map(object({
    type  = string
    value = string
  }))
  default = {}
}

variable "arbitration_app_settings" {
  description = "App settings applied to the arbitration App Service."
  type        = map(string)
  default     = {}
}
