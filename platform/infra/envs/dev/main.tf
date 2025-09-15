# main.tf - Module declarations only (NO provider or required_providers block)

locals {
  rg_name           = "rg-${var.project_name}-${var.env_name}"
  acr_name          = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name          = "aks-${var.project_name}-${var.env_name}"
  kv_name           = "kv-${var.project_name}-${var.env_name}"
  log_name          = "log-${var.project_name}-${var.env_name}"
  appi_name         = var.app_insights_name != "" ? var.app_insights_name : "appi-${var.project_name}-${var.env_name}"
  plan_name          = "asp-${var.project_name}-${var.env_name}"
  func_cron_name     = "func-cron-${var.project_name}-${var.env_name}"
  func_external_name = "func-ext-${var.project_name}-${var.env_name}"
  web_name           = "web-${var.project_name}-${var.env_name}"
  arbitration_plan_name = "asp-${var.project_name}-${var.env_name}-arb"
  arbitration_app_name  = "web-${var.project_name}-${var.env_name}-arb"
  storage_data_name = lower(replace("st${var.project_name}${var.env_name}data", "-", ""))
  sql_server_name   = "sql-${var.project_name}-${var.env_name}"
  aad_app_display   = "aad-${var.project_name}-${var.env_name}"
}

module "rg" {
  source   = "../../Azure/modules/resource-group"
  name     = local.rg_name
  location = var.location
  tags     = var.tags
}

module "dns_zone" {
  source              = "../../Azure/modules/dns-zone"
  zone_name           = var.dns_zone_name
  resource_group_name = module.rg.name
  tags                = var.tags
  a_records           = var.dns_a_records
  cname_records       = var.dns_cname_records
}

module "acr" {
  count               = var.enable_acr ? 1 : 0
  source              = "../../Azure/modules/acr"
  name                = local.acr_name
  resource_group_name = module.rg.name
  location            = var.location
  sku                 = var.acr_sku
  tags                = var.tags
}

module "aks" {
  count               = var.enable_aks ? 1 : 0
  source              = "../../Azure/modules/aks"
  name                = local.aks_name
  resource_group_name = module.rg.name
  location            = var.location
  node_count          = var.aks_node_count
  vm_size             = var.aks_vm_size
  log_analytics_workspace_id = module.app_insights.log_analytics_workspace_id
  tags                = var.tags
}

module "sql" {
  count                         = var.enable_sql ? 1 : 0
  source                        = "../../Azure/modules/sql-serverless"
  server_name                   = local.sql_server_name
  db_name                       = var.sql_db_name
  resource_group_name           = module.rg.name
  location                      = var.location
  admin_login                   = var.sql_admin_login
  admin_password                = var.sql_admin_password
  public_network_access_enabled = var.sql_public_network_access
  sku_name                      = var.sql_sku_name
  auto_pause_delay_in_minutes   = var.sql_auto_pause_minutes
  max_size_gb                   = var.sql_max_size_gb
  tags                          = var.tags
}

module "aad_app" {
  source       = "../../Azure/modules/aad-app"
  display_name = local.aad_app_display
}

module "kv" {
  source                        = "../../Azure/modules/key-vault"
  name                          = local.kv_name
  resource_group_name           = module.rg.name
  location                      = var.location
  public_network_access_enabled = var.kv_public_network_access
  tags                          = var.tags
}

module "app_insights" {
  source                       = "../../Azure/modules/app-insights"
  resource_group_name          = coalesce(var.app_insights_resource_group_name, module.rg.name)
  location                     = var.location
  log_analytics_workspace_name = local.log_name
  application_insights_name    = local.appi_name
  tags                         = var.tags
}

module "func_cron" {
  source                         = "../../Azure/modules/function-app"
  name                           = local.func_cron_name
  plan_name                      = local.plan_name
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.func_plan_sku
  runtime                        = var.function_cron_runtime
  app_insights_connection_string = module.app_insights.application_insights_connection_string
  log_analytics_workspace_id     = module.app_insights.log_analytics_workspace_id
  tags                           = var.tags
}

module "func_external" {
  source                         = "../../Azure/modules/function-app"
  name                           = local.func_external_name
  plan_name                      = local.plan_name
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.func_plan_sku
  runtime                        = var.function_external_runtime
  app_insights_connection_string = module.app_insights.application_insights_connection_string
  log_analytics_workspace_id     = module.app_insights.log_analytics_workspace_id
  tags                           = var.tags
}

module "web" {
  source                         = "../../Azure/modules/app-service-web"
  name                           = local.web_name
  plan_name                      = local.plan_name
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.web_plan_sku
  dotnet_version                 = var.web_dotnet_version
  app_insights_connection_string = module.app_insights.application_insights_connection_string
  log_analytics_workspace_id     = module.app_insights.log_analytics_workspace_id
  tags                           = var.tags
}

module "arbitration_app" {
  source                         = "../../Azure/modules/app-service-arbitration"
  name                           = local.arbitration_app_name
  plan_name                      = local.arbitration_plan_name
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.arbitration_plan_sku
  runtime_stack                  = var.arbitration_runtime_stack
  runtime_version                = var.arbitration_runtime_version
  app_insights_connection_string = module.app_insights.application_insights_connection_string
  log_analytics_workspace_id     = module.app_insights.log_analytics_workspace_id
  connection_strings             = var.arbitration_connection_strings
  app_settings                   = var.arbitration_app_settings
  run_from_package               = var.arbitration_run_from_package
  tags                           = var.tags
}

module "storage_data" {
  count               = var.enable_storage ? 1 : 0
  source              = "../../Azure/modules/storage-account"
  name                = local.storage_data_name
  resource_group_name = module.rg.name
  location            = var.location
  tags                = var.tags
}

output "app_insights_connection_string" {
  value = module.app_insights.application_insights_connection_string
}

output "app_insights_instrumentation_key" {
  value = module.app_insights.application_insights_instrumentation_key
}

output "log_analytics_workspace_id" {
  value = module.app_insights.log_analytics_workspace_id
}

output "sql_server_fqdn" {
  value = var.enable_sql ? module.sql[0].server_fqdn : null
}
