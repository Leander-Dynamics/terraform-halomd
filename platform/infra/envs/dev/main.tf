module "artbit" {
  source = "../../Azure/modules/artbit"

  providers = {
    azurerm     = azurerm
    azurerm.hub = azurerm.hub
  }

  project_name = var.project_name
  env_name     = var.env_name
  tags         = var.tags

  enable_key_vault_private_endpoint = var.enable_key_vault_private_endpoint
  vault_dns_zone_name               = var.vault_dns_zone_name
  vault_dns_resource_group_name     = var.vault_dns_resource_group_name

  environment       = var.environment
  environment_label = var.environment_label
  region            = var.region
  env_region        = var.env_region
  region_short      = var.region_short
  ipv4_prefix       = var.ipv4_prefix

  subscription_id     = var.subscription_id
  vnet_resource_group = var.vnet_resource_group
  main_vnet           = var.main_vnet

  function_dns_zone_name           = var.function_dns_zone_name
  function_dns_resource_group_name = var.function_dns_resource_group_name

  vpns_ipv4                     = var.vpns_ipv4
  mpower_brief_avd_pool_ipv4    = var.mpower_brief_avd_pool_ipv4
  briefbuilder_development_vdis = var.briefbuilder_development_vdis
  monitoring_ipv4               = var.monitoring_ipv4
  octopus_ipv4                  = var.octopus_ipv4

  private_applications_subnet = var.private_applications_subnet

  workflow_storage_account_docs              = var.workflow_storage_account_docs
  workflow_storage_account_cron_function     = var.workflow_storage_account_cron_function
  workflow_storage_account_external_function = var.workflow_storage_account_external_function

  workflow_sqlserver_administrator_login = var.workflow_sqlserver_administrator_login
  workflow_sqlserver_dbadmin_password    = var.workflow_sqlserver_dbadmin_password
  sql_ad_admin_login_username            = var.sql_ad_admin_login_username
  sql_ad_admin_object_id                 = var.sql_ad_admin_object_id
  sql_ad_admin_tenant_id                 = var.sql_ad_admin_tenant_id

  ml_virtual_machine_count          = var.ml_virtual_machine_count
  ml_virtual_machine_size           = var.ml_virtual_machine_size
  ml_virtual_machine_admin_username = var.ml_virtual_machine_admin_username

  enable_redis = var.enable_redis
}

# Optional AKS + ACR (disabled until var.enable_aks = true)
module "aks" {
  source    = "../../Azure/modules/aks"
  providers = { azurerm = azurerm }
  count     = var.enable_aks ? 1 : 0

  resource_group_name = module.artbit.resource_group_name
  location            = var.region
  cluster_name        = var.aks_cluster_name != "" ? var.aks_cluster_name : format("%s-aks", var.env_name)
  dns_prefix          = var.aks_dns_prefix != "" ? var.aks_dns_prefix : format("%s-aks", var.env_name)
  node_count          = var.aks_node_count
  vm_size             = var.aks_vm_size
  os_disk_size_gb     = var.aks_node_os_disk_size_gb
  acr_name            = var.acr_name
  acr_sku             = var.acr_sku
  tags                = var.tags

  # Optional add-ons
  enable_istio_service_mesh          = var.enable_aks_istio
  enable_ingress_application_gateway = var.enable_aks_agw_ingress && var.enable_app_gateway ? true : false
  ingress_application_gateway_id     = var.enable_aks_agw_ingress && var.enable_app_gateway ? try(module.app_gateway[0].id, "") : ""

  # (Planned) Cluster Autoscaler inputs (future wiring inside module)
  enable_cluster_autoscaler = var.enable_aks_cluster_autoscaler
  min_count                 = var.aks_min_count
  max_count                 = var.aks_max_count

  # Autoscaler profile tuning
  enable_auto_scaler_profile               = var.enable_aks_auto_scaler_profile
  auto_scaler_expander                     = var.aks_auto_scaler_expander
  auto_scaler_scan_interval                = var.aks_auto_scaler_scan_interval
  auto_scaler_balance_similar_node_groups  = var.aks_auto_scaler_balance_similar_node_groups
  auto_scaler_max_graceful_termination_sec = var.aks_auto_scaler_max_graceful_termination_sec
}

# Application Gateway for AKS ingress (AGIC). Deployed when enabled.
locals {
  agw_name = var.app_gateway_name != "" ? var.app_gateway_name : format("agw-%s-%s-%s", var.env_name, var.region_short, var.project_name)
  agw_subnet_id = format(
    "/subscriptions/%s/resourceGroups/%s/providers/Microsoft.Network/virtualNetworks/%s/subnets/%s",
    var.subscription_id,
    var.vnet_resource_group,
    var.main_vnet,
    format("%s-private-services-snet-1", var.env_region)
  )
  agw_fqdn_label = lower(replace(format("%s-%s-agw", var.env_name, var.region_short), "_", ""))
}

module "app_gateway" {
  source              = "../../Azure/modules/app-gateway"
  count               = var.enable_app_gateway ? 1 : 0
  name                = local.agw_name
  resource_group_name = module.artbit.resource_group_name
  location            = var.region
  subnet_id           = local.agw_subnet_id
  fqdn_prefix         = local.agw_fqdn_label

  # AGIC will manage listeners and backends; don't create defaults
  create_default_listener = false
  backend_fqdns           = []
  tags                    = var.tags
}

# Azure Front Door with WAF at the edge, forwarding to the Application Gateway public FQDN
module "frontdoor_waf" {
  source              = "../../modules/frontdoor_waf"
  count               = var.enable_frontdoor && var.enable_app_gateway ? 1 : 0
  resource_group_name = module.artbit.resource_group_name
  profile_name        = var.frontdoor_profile_name != "" ? var.frontdoor_profile_name : format("fd-%s-%s-%s", var.env_name, var.region_short, var.project_name)
  endpoint_name       = var.frontdoor_endpoint_name != "" ? var.frontdoor_endpoint_name : format("fde-%s-%s-%s", var.env_name, var.region_short, var.project_name)
  origin_host_name    = try(module.app_gateway[0].public_ip_fqdn, null)
  tags                = var.tags
}

output "resource_group_name" {
  description = "Resource group provisioned for the environment."
  value       = module.artbit.resource_group_name
}

output "resource_group_id" {
  description = "Resource ID of the workflow resource group."
  value       = module.artbit.resource_group_id
}

output "frontend_default_hostname" {
  description = "Default hostname assigned to the frontend web application."
  value       = module.artbit.frontend_default_hostname
}

output "backend_default_hostname" {
  description = "Default hostname assigned to the backend web application."
  value       = module.artbit.backend_default_hostname
}

output "cron_function_default_hostname" {
  description = "Default hostname assigned to the cron function application."
  value       = module.artbit.cron_function_default_hostname
}

output "external_function_default_hostname" {
  description = "Default hostname assigned to the external function application."
  value       = module.artbit.external_function_default_hostname
}

output "frontend_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the frontend web application."
  value       = module.artbit.frontend_private_endpoint_ip
}

output "backend_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the backend web application."
  value       = module.artbit.backend_private_endpoint_ip
}

output "cron_function_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the cron function application."
  value       = module.artbit.cron_function_private_endpoint_ip
}

output "storage_accounts" {
  description = "Workflow storage account identifiers and secrets."
  value       = module.artbit.storage_accounts
  sensitive   = true
}


output "sql_server_details" {
  description = "Details for the workflow SQL server and associated databases."
  value       = module.artbit.sql_server_details
}

output "load_balancer_details" {
  description = "Identifiers for the public load balancer."
  value       = module.artbit.load_balancer_details
}

output "ml_virtual_machine_private_ips" {
  description = "Private IPv4 addresses allocated to ML virtual machines."
  value       = module.artbit.ml_virtual_machine_private_ips
}

output "openai_endpoint" {
  description = "Endpoint URL for the Azure OpenAI account."
  value       = module.artbit.openai_endpoint
}

output "openai_primary_key" {
  description = "Primary access key for the Azure OpenAI account."
  value       = module.artbit.openai_primary_key
  sensitive   = true
}

# AKS/ACR outputs (if enabled)
output "aks_id" {
  description = "AKS cluster resource ID"
  value       = try(module.aks[0].aks_id, null)
}

output "acr_login_server" {
  description = "ACR login server hostname"
  value       = try(module.aks[0].acr_login_server, null)
}
