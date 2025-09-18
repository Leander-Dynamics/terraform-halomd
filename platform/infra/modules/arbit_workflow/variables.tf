variable "project_name" {
  type        = string
  description = "Project identifier used for naming resources."
}

variable "env_name" {
  type        = string
  description = "Environment short name (e.g. dev, qa, stage, prod)."
}

variable "tags" {
  type        = map(string)
  description = "Common tags applied to all resources."
  default     = {}
}

variable "environment" {
  type        = string
  description = "Human friendly environment label (e.g. dev)."
}

variable "environment_label" {
  type        = string
  description = "Display label for the environment."
}

variable "region" {
  type        = string
  description = "Azure region where resources are deployed."
}

variable "env_region" {
  type        = string
  description = "Environment and region composite name (e.g. dev-eus2)."
}

variable "region_short" {
  type        = string
  description = "Short code for the Azure region (e.g. eus2)."
}

variable "ipv4_prefix" {
  type        = string
  description = "IPv4 prefix used when deriving static addresses."
}

variable "azure_vpn_ipv4" {
  type        = string
  description = "Azure VPN IPv4 CIDR block."
  default     = ""
}

variable "sonicwall_vpn_ipv4" {
  type        = string
  description = "SonicWall VPN IPv4 CIDR block."
  default     = ""
}

variable "point_to_site_vpn_ipv4" {
  type        = string
  description = "Point-to-site VPN IPv4 CIDR block."
  default     = ""
}

variable "vpns_ipv4" {
  type        = list(string)
  description = "List of VPN IPv4 CIDR blocks allowed through NSGs."
}

variable "vdis_ipv4" {
  type        = list(string)
  description = "List of VDI IPv4 CIDR blocks."
  default     = []
}

variable "mpower_brief_avd_pool_ipv4" {
  type        = list(string)
  description = "MPOWER brief AVD pool IPv4 ranges."
  default     = []
}

variable "briefbuilder_development_vdis" {
  type        = list(string)
  description = "Briefbuilder development VDI IPv4 ranges."
  default     = []
}

variable "halomd_development_test_vdi" {
  type        = list(string)
  description = "HaloMD development/test VDI IPv4 ranges."
  default     = []
}

variable "halomd_brief_avd_vnet_ipv4" {
  type        = list(string)
  description = "HaloMD brief AVD VNet IPv4 ranges."
  default     = []
}

variable "monitoring_ipv4" {
  type        = string
  description = "Prometheus monitoring IPv4 CIDR block."
}

variable "octopus_ipv4" {
  type        = string
  description = "Octopus deploy IPv4 address."
}

variable "builder_ipv4" {
  type        = string
  description = "Builder IPv4 address."
  default     = ""
}

variable "dagster_ipv4" {
  type        = string
  description = "Dagster IPv4 address."
  default     = ""
}

variable "public_operations_subnet" {
  type        = string
  description = "Public operations subnet prefix."
  default     = ""
}

variable "public_gateways_subnet" {
  type        = string
  description = "Public gateways subnet prefix."
  default     = ""
}

variable "private_asps_subnet" {
  type        = string
  description = "Private ASPs subnet prefix."
  default     = ""
}

variable "private_gateways_subnet" {
  type        = string
  description = "Private gateways subnet prefix."
  default     = ""
}

variable "private_applications_subnet" {
  type        = string
  description = "Private applications subnet prefix used for ML VMs."
}

variable "private_services_subnet" {
  type        = string
  description = "Private services subnet prefix."
  default     = ""
}

variable "private_powerplatform_subnet" {
  type        = string
  description = "Private Power Platform subnet prefix."
  default     = ""
}

variable "private_psql_databases_subnet" {
  type        = string
  description = "Private PostgreSQL databases subnet prefix."
  default     = ""
}

variable "private_dataplatform_subnet" {
  type        = string
  description = "Private data platform subnet prefix."
  default     = ""
}

variable "private_operations_subnet" {
  type        = string
  description = "Private operations subnet prefix."
  default     = ""
}

variable "public_mssql_databases_subnet" {
  type        = string
  description = "Public MSSQL subnet prefix."
  default     = ""
}

variable "private_databases_subnet" {
  type        = string
  description = "Private databases subnet prefix."
  default     = ""
}

variable "subscription_id" {
  type        = string
  description = "Subscription ID hosting the workflow workload."
}

variable "vnet_resource_group" {
  type        = string
  description = "Resource group that owns the shared virtual network."
}

variable "main_vnet" {
  type        = string
  description = "Name of the shared virtual network."
}

variable "function_dns_zone_name" {
  type        = string
  description = "Private DNS zone name hosting Azure Web Apps records."
}

variable "function_dns_resource_group_name" {
  type        = string
  description = "Resource group containing the private DNS zone."
}

variable "workflow_storage_account_docs" {
  type        = string
  description = "Globally unique storage account name for documentation."
}

variable "workflow_storage_account_cron_function" {
  type        = string
  description = "Globally unique storage account name for cron functions."
}

variable "workflow_storage_account_external_function" {
  type        = string
  description = "Globally unique storage account name for external functions."
}

variable "workflow_sqlserver_administrator_login" {
  type        = string
  description = "SQL administrator login name for the workflow server."
  default     = "dbadmin"
}

variable "workflow_sqlserver_dbadmin_password" {
  type        = string
  description = "SQL administrator password for the workflow server."
  sensitive   = true
}

variable "sql_ad_admin_login_username" {
  type        = string
  description = "Azure AD admin login username for SQL."
  default     = "SQL Admins"
}

variable "sql_ad_admin_object_id" {
  type        = string
  description = "Azure AD admin object ID for SQL."
  default     = "b846eec0-b0b9-40d4-a1e3-2fbaa8e83905"
}

variable "sql_ad_admin_tenant_id" {
  type        = string
  description = "Tenant ID used for the Azure AD SQL administrator."
  default     = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"
}

variable "ml_virtual_machine_count" {
  type        = number
  description = "Number of ML virtual machines to provision."
  default     = 2
}

variable "ml_virtual_machine_size" {
  type        = string
  description = "Size of the ML virtual machines."
  default     = "Standard_D2s_v4"
}

variable "ml_virtual_machine_admin_username" {
  type        = string
  description = "Admin username for the ML virtual machines."
  default     = "adminuser"
}
