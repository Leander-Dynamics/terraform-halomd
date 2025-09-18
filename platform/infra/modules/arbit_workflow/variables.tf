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

variable "vpns_ipv4" {
  type        = list(string)
  description = "List of VPN IPv4 CIDR blocks allowed through NSGs."
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

variable "monitoring_ipv4" {
  type        = string
  description = "Prometheus monitoring IPv4 CIDR block."
}

variable "octopus_ipv4" {
  type        = string
  description = "Octopus deploy IPv4 address."
}

variable "private_applications_subnet" {
  type        = string
  description = "Private applications subnet prefix used for ML VMs."
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
