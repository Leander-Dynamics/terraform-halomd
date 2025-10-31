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
