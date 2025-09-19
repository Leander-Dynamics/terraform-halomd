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
