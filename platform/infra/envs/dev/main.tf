module "artbit" {
  source = "../../Azure/modules/artbit"

  providers = {
    azurerm     = azurerm
    azurerm.hub = azurerm.hub
  }

  project_name = var.project_name
  env_name     = var.env_name
  tags         = var.tags

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
}

output "resource_group_name" {
  description = "Resource group provisioned for the environment."
  value       = module.arbit_workflow.resource_group_name
}

output "resource_group_id" {
  description = "Resource ID of the workflow resource group."
  value       = module.arbit_workflow.resource_group_id
}

output "frontend_default_hostname" {
  description = "Default hostname assigned to the frontend web application."
  value       = module.arbit_workflow.frontend_default_hostname
}

output "backend_default_hostname" {
  description = "Default hostname assigned to the backend web application."
  value       = module.arbit_workflow.backend_default_hostname
}

output "cron_function_default_hostname" {
  description = "Default hostname assigned to the cron function application."
  value       = module.arbit_workflow.cron_function_default_hostname
}

output "external_function_default_hostname" {
  description = "Default hostname assigned to the external function application."
  value       = module.arbit_workflow.external_function_default_hostname
}

output "frontend_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the frontend web application."
  value       = module.arbit_workflow.frontend_private_endpoint_ip
}

output "backend_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the backend web application."
  value       = module.arbit_workflow.backend_private_endpoint_ip
}

output "cron_function_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the cron function application."
  value       = module.arbit_workflow.cron_function_private_endpoint_ip
}

output "storage_accounts" {
  description = "Workflow storage account identifiers and secrets."
  value       = module.arbit_workflow.storage_accounts
  sensitive   = true
}

output "redis_cache_details" {
  description = "Redis cache identifiers and connection details."
  value       = module.arbit_workflow.redis_cache_details
  sensitive   = true
}

output "sql_server_details" {
  description = "Details for the workflow SQL server and associated databases."
  value       = module.arbit_workflow.sql_server_details
}

output "load_balancer_details" {
  description = "Identifiers for the public load balancer."
  value       = module.arbit_workflow.load_balancer_details
}

output "ml_virtual_machine_private_ips" {
  description = "Private IPv4 addresses allocated to ML virtual machines."
  value       = module.arbit_workflow.ml_virtual_machine_private_ips
}

output "openai_endpoint" {
  description = "Endpoint URL for the Azure OpenAI account."
  value       = module.arbit_workflow.openai_endpoint
}

output "openai_primary_key" {
  description = "Primary access key for the Azure OpenAI account."
  value       = module.arbit_workflow.openai_primary_key
  sensitive   = true
}
