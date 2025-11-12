variable "name" {
  description = "Optional AKS name; if unset, use cluster_name."
  type        = string
  default     = ""
}

variable "cluster_name" {
  description = "AKS cluster name (preferred)."
  type        = string
  default     = ""
}

variable "resource_group_name" {
  description = "Resource group name for AKS and ACR."
  type        = string
}

variable "location" {
  description = "Azure region for AKS and ACR."
  type        = string
}

variable "dns_prefix" {
  description = "DNS prefix for the AKS cluster; defaults to cluster name."
  type        = string
  default     = ""
}

variable "node_count" {
  description = "Node count for the default pool."
  type        = number
  default     = 3
}

variable "enable_cluster_autoscaler" {
  description = "Enable AKS Cluster Autoscaler on the default node pool."
  type        = bool
  default     = false
}

variable "min_count" {
  description = "Minimum node count when cluster autoscaler is enabled."
  type        = number
  default     = 3
}

variable "max_count" {
  description = "Maximum node count when cluster autoscaler is enabled."
  type        = number
  default     = 6
}

variable "vm_size" {
  description = "VM size for default node pool."
  type        = string
  default     = "Standard_D8s_v5" # 8 vCPU, 32 GiB
}

variable "os_disk_size_gb" {
  description = "OS disk size in GB for AKS nodes."
  type        = number
  default     = 512
}

variable "tags" {
  description = "Tags to apply to resources."
  type        = map(string)
  default     = {}
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace resource ID used for Container Insights."
  type        = string
  default     = null
}

variable "acr_name" {
  description = "Optional ACR name; if empty, one will be generated."
  type        = string
  default     = ""
}

variable "acr_sku" {
  description = "SKU for ACR (Basic, Standard, Premium)."
  type        = string
  default     = "Standard"
}

variable "enable_istio_service_mesh" {
  description = "Enable AKS Istio service mesh add-on (service_mesh_profile)."
  type        = bool
  default     = false
}

variable "enable_ingress_application_gateway" {
  description = "Enable AKS Ingress Application Gateway (AGIC) add-on. Requires ingress_application_gateway_id to be set."
  type        = bool
  default     = false
}

variable "ingress_application_gateway_id" {
  description = "Resource ID of an existing Application Gateway to use with the AGIC add-on."
  type        = string
  default     = ""
}

# --- Optional Cluster Autoscaler profile tuning ---
variable "enable_auto_scaler_profile" {
  description = "Enable and configure AKS Cluster Autoscaler profile for faster reaction and better placement."
  type        = bool
  default     = false
}

variable "auto_scaler_expander" {
  description = "Autoscaler expander strategy (least-waste | most-pods | random | priority)."
  type        = string
  default     = "least-waste"
}

variable "auto_scaler_scan_interval" {
  description = "Interval at which autoscaler scans for changes (e.g., 10s, 30s)."
  type        = string
  default     = "10s"
}

variable "auto_scaler_balance_similar_node_groups" {
  description = "Balance similar node groups when scaling."
  type        = bool
  default     = true
}

variable "auto_scaler_max_graceful_termination_sec" {
  description = "Maximum seconds allowed for graceful pod termination during scale down."
  type        = number
  default     = 600
}
