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

variable "vpns_ipv4" {
  description = "List of VPN IPv4 CIDR blocks allowed through network security rules."
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

variable "monitoring_ipv4" {
  description = "Prometheus monitoring IPv4 CIDR block."
  type        = string
}

variable "octopus_ipv4" {
  description = "Octopus Deploy IPv4 address."
  type        = string
}

variable "private_applications_subnet" {
  description = "Private applications subnet prefix used for ML VMs."
  type        = string
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


# --- Added by Option A KV PE toggle ---

variable "enable_key_vault_private_endpoint" {
  description = "When true, provision Key Vault private endpoint and disable public network access."
  type        = bool
  default     = false
}

variable "vault_dns_zone_name" {
  description = "Private DNS zone name for Key Vault (privatelink.vaultcore.azure.net)."
  type        = string
  default     = "privatelink.vaultcore.azure.net"
}

variable "vault_dns_resource_group_name" {
  description = "Resource group name that hosts the Key Vault private DNS zone."
  type        = string
}

variable "enable_redis" {
  description = "When true, provisions Redis cache for the workflow."
  type        = bool
  default     = false
}

# --- AKS (disabled by default until configured) ---

variable "enable_aks" {
  description = "When true, provisions an AKS cluster and ACR for the environment."
  type        = bool
  default     = false
}

variable "aks_cluster_name" {
  description = "AKS cluster name (unique per env)"
  type        = string
  default     = ""
}

variable "aks_dns_prefix" {
  description = "DNS prefix for AKS"
  type        = string
  default     = ""
}

variable "aks_node_count" {
  description = "Node count in default pool"
  type        = number
  default     = 3
}

variable "aks_vm_size" {
  description = "VM size for AKS default pool (8 vCPU, 32GB recommended)"
  type        = string
  default     = "Standard_D8s_v5"
}

variable "aks_node_os_disk_size_gb" {
  description = "Node OS disk size in GB"
  type        = number
  default     = 512
}

variable "acr_name" {
  description = "Optional ACR name; leave empty to auto-generate"
  type        = string
  default     = ""
}

variable "acr_sku" {
  description = "ACR SKU (Basic, Standard, Premium)"
  type        = string
  default     = "Standard"
}

# --- Ingress/Gateway/Front Door toggles (dev) ---

variable "enable_app_gateway" {
  description = "When true, provisions an Application Gateway for AKS ingress (AGIC)."
  type        = bool
  default     = false
}

variable "app_gateway_name" {
  description = "Application Gateway name (optional). If empty, a name will be derived."
  type        = string
  default     = ""
}

variable "enable_frontdoor" {
  description = "When true, provisions Azure Front Door (Standard) with WAF in front of the gateway."
  type        = bool
  default     = false
}

variable "frontdoor_profile_name" {
  description = "Front Door profile name (optional)."
  type        = string
  default     = ""
}

variable "frontdoor_endpoint_name" {
  description = "Front Door endpoint name (optional)."
  type        = string
  default     = ""
}

variable "enable_aks_istio" {
  description = "Enable Istio service mesh add-on for AKS."
  type        = bool
  default     = false
}

variable "enable_aks_agw_ingress" {
  description = "Enable AKS Ingress Application Gateway (AGIC) integration using the created App Gateway."
  type        = bool
  default     = false
}

# --- AKS Cluster Autoscaler (optional) ---
variable "enable_aks_cluster_autoscaler" {
  description = "Enable Cluster Autoscaler on a user node pool (follow-up wiring)."
  type        = bool
  default     = false
}

variable "aks_min_count" {
  description = "Minimum nodes for autoscaler (when enabled)."
  type        = number
  default     = 3
}

variable "aks_max_count" {
  description = "Maximum nodes for autoscaler (when enabled)."
  type        = number
  default     = 6
}

# --- AKS Autoscaler profile tuning (optional) ---
variable "enable_aks_auto_scaler_profile" {
  description = "Enable AKS auto_scaler_profile for faster reaction to load."
  type        = bool
  default     = false
}

variable "aks_auto_scaler_expander" {
  description = "Autoscaler expander strategy."
  type        = string
  default     = "least-waste"
}

variable "aks_auto_scaler_scan_interval" {
  description = "Autoscaler scan interval (e.g., 10s)."
  type        = string
  default     = "10s"
}

variable "aks_auto_scaler_balance_similar_node_groups" {
  description = "Balance similar node groups during scaling."
  type        = bool
  default     = true
}

variable "aks_auto_scaler_max_graceful_termination_sec" {
  description = "Maximum graceful termination seconds during scale down."
  type        = number
  default     = 600
}
