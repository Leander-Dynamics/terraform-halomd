# -------------------------
# Connectivity
# -------------------------

variable "enable_nat_gateway" {
  description = "Flag to deploy a NAT Gateway for outbound connectivity."
  type        = bool
  default     = false

  validation {
    condition     = var.enable_nat_gateway == false || var.nat_gateway_configuration != null
    error_message = "nat_gateway_configuration must be provided when enable_nat_gateway is true."
  }
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

variable "enable_vpn_gateway" {
  description = "Flag to deploy a virtual network gateway for hybrid connectivity."
  type        = bool
  default     = false

  validation {
    condition     = var.enable_vpn_gateway == false || var.vpn_gateway_configuration != null
    error_message = "vpn_gateway_configuration must be provided when enable_vpn_gateway is true."
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

# -------------------------
# Bastion
# -------------------------
variable "enable_bastion" {
  description = "Flag to deploy an Azure Bastion host."
  type        = bool
  default     = false
}

variable "bastion_subnet_key" {
  description = "Key referencing the AzureBastionSubnet entry in the `subnets` map."
  type        = string
  default     = null

  validation {
    condition     = var.enable_bastion == false || try(trimspace(var.bastion_subnet_key), "") != ""
    error_message = "bastion_subnet_key must be provided when enable_bastion is true."
  }
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

variable "subnet_network_security_rules" {
  description = <<-DOC
  Map of network security rule sets keyed by subnet name. Each entry should
  match the `security_rules` input for the `network-security-group` module and
  defaults to an empty map, resulting in only the built-in Azure NSG rules.
  DOC
  type = map(map(object({
    priority                     = number
    direction                    = optional(string, "Inbound")
    access                       = optional(string, "Allow")
    protocol                     = optional(string, "*")
    source_port_range            = optional(string)
    source_port_ranges           = optional(list(string))
    destination_port_range       = optional(string)
    destination_port_ranges      = optional(list(string))
    source_address_prefix        = optional(string)
    source_address_prefixes      = optional(list(string))
    destination_address_prefix   = optional(string)
    destination_address_prefixes = optional(list(string))
    description                  = optional(string)
  })))
  default = {}
}

# -------------------------
# Key Vault & Storage networking
# -------------------------
variable "kv_public_network_access" {
  description = "Allow public network access to the Key Vault."
  type        = bool
  default     = true
}

variable "kv_network_acls" {
  description = "Optional network ACL configuration for the Key Vault."
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
  description = "Subnet key used when creating the Key Vault private endpoint."
  type        = string
  default     = null
}

variable "kv_private_dns_zone_ids" {
  description = "Private DNS zone IDs linked to the Key Vault private endpoint."
  type        = list(string)
  default     = []
}

variable "kv_private_endpoint_resource_id" {
  description = "Override resource ID supplied to the Key Vault private endpoint module."
  type        = string
  default     = null
}

variable "enable_storage_private_endpoint" {
  description = "Toggle creation of a private endpoint for the storage account."
  type        = bool
  default     = false
}

variable "storage_private_endpoint_subnet_key" {
  description = "Subnet key used when creating the storage account private endpoint."
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
# SQL Database
# -------------------------
variable "sql_database_name" {
  description = "Optional override for the SQL database name. Defaults to <project>-<env> when blank."
  type        = string
  default     = ""
}

variable "sql_admin_login" {
  description = "Administrator login for the SQL server. Supply via secure pipeline variables or Key Vault."
  type        = string
}

variable "sql_admin_password" {
  description = "Administrator password for the SQL server. Supply via secure pipeline variables or Key Vault."
  type        = string
  sensitive   = true
}

variable "sql_sku_name" {
  description = "SKU name for the serverless SQL database (for example GP_S_Gen5_2)."
  type        = string
  default     = "GP_S_Gen5_2"
}

variable "sql_max_size_gb" {
  description = "Maximum size of the SQL database in gigabytes."
  type        = number
  default     = 75
}

variable "sql_auto_pause_delay" {
  description = "Number of minutes before the serverless database auto pauses. Use -1 to disable."
  type        = number
  default     = 60
}

variable "sql_min_capacity" {
  description = "Minimum vCore capacity for the serverless database."
  type        = number
  default     = 0.5
}

variable "sql_max_capacity" {
  description = "Maximum vCore capacity for the serverless database."
  type        = number
  default     = 4
}

variable "sql_public_network_access" {
  description = "Whether public network access is enabled for the SQL server."
  type        = bool
  default     = true
}

variable "sql_firewall_rules" {
  description = "List of firewall rules to apply to the SQL server."
  type = list(object({
    name             = string
    start_ip_address = string
    end_ip_address   = string
  }))
  default = []
}
