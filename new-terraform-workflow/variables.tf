# First the generic stuff
variable "environment" {
  description = "Environment"
  type        = string
  sensitive   = false
}

variable "environment_label" {
  description = "Environment Label"
  type        = string
  sensitive   = false
}

variable "region" {
  description = "Environment region only"
  type        = string
  sensitive   = false
}


variable "env_region" {
  description = "Environment region concatenated"
  type        = string
  sensitive   = false
}

variable "region_short" {
  description = "Environment region short"
  type        = string
  sensitive   = false
}

variable "ipv4_prefix" {
  description = "ipv4 prefix"
  type        = string
  sensitive   = false
}

variable "azure_vpn_ipv4" {
  description = "Azure VPN IPv4 CIDR block"
  type        = string
}

variable "sonicwall_vpn_ipv4" {
  description = "SonicWall VPN IPv4 CIDR block"
  type        = string
}

variable "point_to_site_vpn_ipv4" {
  description = "Point-to-site VPN IPv4 CIDR block"
  type        = string
}

variable "vpns_ipv4" {
  description = "List of VPN IPv4 CIDR blocks"
  type        = list(string)
}

variable "vdis_ipv4" {
  description = "List of VDI IPv4 CIDR blocks"
  type        = list(string)
}

variable "mpower_brief_avd_pool_ipv4" {
  description = "MPOWER brief AVD pool IPv4 CIDR "
  type        = list(string)
}

variable "briefbuilder_development_vdis" {
  description = "Briefbuilder development VDIs"
  type        = list(string)
}

variable "halomd_development_test_vdi" {
  description = "HaloMD development/test VDI IPv4"
  type        = list(string)
}

variable "halomd_brief_avd_vnet_ipv4" {
  description = "HaloMD brief AVD VNet IPv4 "
  type        = list(string)
}

variable "monitoring_ipv4" {
  description = "Prometheus monitoring IPv4 "
  type        = string

}

variable "octopus_ipv4" {
  description = "Octopus IPv4 address"
  type        = string
}

variable "builder_ipv4" {
  description = "Builder IPv4 address "
  type        = string
}

variable "dagster_ipv4" {
  description = "Dagster IPv4 address"
  type        = string
}

# here come the subnet vars
variable "public_operations_subnet" {
  description = "Public operations subnet prefix"
  type        = string
}

variable "public_gateways_subnet" {
  description = "Public gateways subnet prefix"
  type        = string
}

variable "private_asps_subnet" {
  description = "Private ASPs subnet prefix"
  type        = string
}

variable "private_gateways_subnet" {
  description = "Private gateways subnet prefix"
  type        = string
}

variable "private_applications_subnet" {
  description = "Private applications subnet prefix"
  type        = string
}

variable "private_services_subnet" {
  description = "Private services subnet prefix"
  type        = string
}

variable "private_powerplatform_subnet" {
  description = "Private Power Platform subnet prefix"
  type        = string
}

variable "private_psql_databases_subnet" {
  description = "Private PostgreSQL databases subnet prefix"
  type        = string
}

variable "private_dataplatform_subnet" {
  description = "Private data platform subnet prefix"
  type        = string
}

variable "private_operations_subnet" {
  description = "Private operations subnet prefix"
  type        = string
}

variable "public_mssql_databases_subnet" {
  description = "Public MSSQL subnet prefix"
  type        = string
}

variable "private_databases_subnet" {
  description = "Private databases subnet prefix"
  type        = string
}
# end of subnet vars


variable "subscription_id" {
  description = "Subscription id"
  type        = string
}


variable "vnet_resource_group" {
  description = "Vnet Resource Group"
  type        = string
}

variable "main_vnet" {
  description = "Main Vnet Name"
  type        = string
}

# now the more specific stuff related to workflow
variable "workflow_storage_account_docs" {
  description = "Storage account name"
  type        = string
  sensitive   = false
}

# now the more specific stuff related to workflow
variable "workflow_storage_account_cron_function" {
  description = "Storage account name"
  type        = string
  sensitive   = false
}

variable "workflow_storage_account_external_function" {
  description = "Storage account name"
  type        = string
  sensitive   = false
}

# workflow SQL Server Database
variable "workflow_sqlserver_dbadmin_password" {
  description = "workflow database password"
  type        = string
  sensitive   = true
}

