variable "project_name" {
  description = "Project or application identifier used for naming."
  type        = string
}

variable "env_name" {
  description = "Environment name (e.g. dev, qa, stage, prod)."
  type        = string
}

variable "tags" {
  description = "Tags applied to all workflow resources."
  type        = map(string)
}

variable "subscription_id" {
  description = "Azure subscription ID hosting the workflow workload."
  type        = string
}

variable "hub_subscription_id" {
  description = "Subscription ID for the networking hub where private DNS zones live."
  type        = string
}

variable "tenant_id" {
  description = "Azure tenant ID used for authentication."
  type        = string
  default     = ""
}

variable "environment" {
  description = "Environment identifier passed to the workflow module."
  type        = string
}

variable "environment_label" {
  description = "Human friendly environment label used for tagging."
  type        = string
}

variable "region" {
  description = "Azure region where resources are deployed."
  type        = string
}

variable "env_region" {
  description = "Composite environment/region string (e.g. dev-eus2)."
  type        = string
}

variable "region_short" {
  description = "Short region code (e.g. eus2)."
  type        = string
}

variable "ipv4_prefix" {
  description = "IPv4 prefix used for static addressing."
  type        = string
}

variable "vnet_resource_group" {
  description = "Resource group containing the shared virtual network."
  type        = string
}

variable "main_vnet" {
  description = "Name of the shared virtual network hosting workflow subnets."
  type        = string
}

variable "function_dns_zone_name" {
  description = "Private DNS zone name for Azure Web Apps."
  type        = string
}

variable "function_dns_resource_group_name" {
  description = "Resource group containing the private DNS zone."
  type        = string
}

variable "azure_vpn_ipv4" {
  description = "Azure VPN IPv4 CIDR block."
  type        = string
  default     = ""
}

variable "sonicwall_vpn_ipv4" {
  description = "SonicWall VPN IPv4 CIDR block."
  type        = string
  default     = ""
}

variable "point_to_site_vpn_ipv4" {
  description = "Point-to-site VPN IPv4 CIDR block."
  type        = string
  default     = ""
}

variable "vpns_ipv4" {
  description = "List of VPN IPv4 CIDR blocks allowed through network security rules."
  type        = list(string)
  default     = []
}

variable "vdis_ipv4" {
  description = "List of VDI IPv4 CIDR blocks."
  type        = list(string)
  default     = []
}

variable "mpower_brief_avd_pool_ipv4" {
  description = "MPOWER brief AVD pool IPv4 CIDR ranges."
  type        = list(string)
  default     = []
}

variable "briefbuilder_development_vdis" {
  description = "Briefbuilder development VDI IPv4 ranges."
  type        = list(string)
  default     = []
}

variable "halomd_development_test_vdi" {
  description = "HaloMD development/test VDI IPv4 ranges."
  type        = list(string)
  default     = []
}

variable "halomd_brief_avd_vnet_ipv4" {
  description = "HaloMD brief AVD virtual network IPv4 ranges."
  type        = list(string)
  default     = []
}

variable "monitoring_ipv4" {
  description = "Prometheus monitoring IPv4 CIDR block."
  type        = string
}

variable "octopus_ipv4" {
  description = "Octopus Deploy IPv4 address."
  type        = string
}

variable "builder_ipv4" {
  description = "Builder IPv4 address."
  type        = string
  default     = ""
}

variable "dagster_ipv4" {
  description = "Dagster IPv4 address."
  type        = string
  default     = ""
}

variable "public_operations_subnet" {
  description = "Public operations subnet prefix."
  type        = string
  default     = ""
}

variable "public_gateways_subnet" {
  description = "Public gateways subnet prefix."
  type        = string
  default     = ""
}

variable "private_asps_subnet" {
  description = "Private ASPs subnet prefix."
  type        = string
  default     = ""
}

variable "private_gateways_subnet" {
  description = "Private gateways subnet prefix."
  type        = string
  default     = ""
}

variable "private_applications_subnet" {
  description = "Private applications subnet prefix used for ML VMs."
  type        = string
}

variable "private_services_subnet" {
  description = "Private services subnet prefix."
  type        = string
  default     = ""
}

variable "private_powerplatform_subnet" {
  description = "Private Power Platform subnet prefix."
  type        = string
  default     = ""
}

variable "private_psql_databases_subnet" {
  description = "Private PostgreSQL databases subnet prefix."
  type        = string
  default     = ""
}

variable "private_dataplatform_subnet" {
  description = "Private data platform subnet prefix."
  type        = string
  default     = ""
}

variable "private_operations_subnet" {
  description = "Private operations subnet prefix."
  type        = string
  default     = ""
}

variable "public_mssql_databases_subnet" {
  description = "Public MSSQL subnet prefix."
  type        = string
  default     = ""
}

variable "private_databases_subnet" {
  description = "Private databases subnet prefix."
  type        = string
  default     = ""
}

variable "workflow_storage_account_docs" {
  description = "Storage account name for workflow documentation assets."
  type        = string
}

variable "workflow_storage_account_cron_function" {
  description = "Storage account name backing the cron function app."
  type        = string
}

variable "workflow_storage_account_external_function" {
  description = "Storage account name backing the external function app."
  type        = string
}

variable "workflow_sqlserver_administrator_login" {
  description = "SQL administrator login name for the workflow server."
  type        = string
  default     = "dbadmin"
}

variable "workflow_sqlserver_dbadmin_password" {
  description = "SQL administrator password for the workflow server."
  type        = string
  sensitive   = true
}

variable "sql_ad_admin_login_username" {
  description = "Azure AD admin login username for SQL."
  type        = string
  default     = "SQL Admins"
}

variable "sql_ad_admin_object_id" {
  description = "Azure AD admin object ID for SQL."
  type        = string
  default     = "b846eec0-b0b9-40d4-a1e3-2fbaa8e83905"
}

variable "sql_ad_admin_tenant_id" {
  description = "Tenant ID used for the Azure AD SQL administrator."
  type        = string
  default     = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"
}

variable "ml_virtual_machine_count" {
  description = "Number of ML virtual machines to provision."
  type        = number
  default     = 2
}

variable "ml_virtual_machine_size" {
  description = "Size of the ML virtual machines."
  type        = string
  default     = "Standard_D2s_v4"
}

variable "ml_virtual_machine_admin_username" {
  description = "Admin username for the ML virtual machines."
  type        = string
  default     = "adminuser"
}
