
# variables.tf - Cleaned and safe structure

variable "location" {
  description = "Azure region"
  type        = string
}

variable "env_name" {
  description = "Environment name (dev, stage, prod)"
  type        = string
}

variable "project_name" {
  description = "Project prefix (e.g. arbit)"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "storage_account_name" {
  description = "Storage account for backend state"
  type        = string
}

variable "container_name" {
  description = "Container for backend state"
  type        = string
}

variable "key" {
  description = "Key (file name) of the backend state"
  type        = string
}

variable "use_azuread_auth" {
  description = "Whether to use Azure AD authentication for the backend"
  type        = bool
  default     = true
}
variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "tenant_id" {
  description = "Azure Tenant ID"
  type        = string
}
variable "deploy_rg" {
  type        = string
  description = "Resource Group name"
}
variable "app_insights_connection_string" {
  type        = string
  description = "App Insights connection string"
}
variable "app_insights_name" {
  type        = string
  description = "App Insights name"
}
variable "app_insights_rg" {
  type        = string
  description = "App Insights Resource Group name"
}

