locals {
  rg_name   = "rg-${var.project_name}-${var.env}"
  kv_name   = "kv-${var.project_name}-${var.env}"
  log_name  = "log-${var.project_name}-${var.env}"
  appi_name = "appi-${var.project_name}-${var.env}"

  acr_name  = lower(replace("acr${var.project_name}${var.env}", "-", ""))
  aks_name  = "aks-${var.project_name}-${var.env}-${var.location}"

  service_plan_name  = "asp-${var.project_name}-${var.env}-${var.location}"
  web_name           = "app-halomdweb-${var.env}"
  func_external_name = "func-external-${var.env}"
  func_cron_name     = "func-cron-${var.env}"

  storage_data_name  = lower(replace("st${var.project_name}${var.env}data", "-", ""))
  sql_server_name    = "sql-${var.project_name}-${var.env}"

  aad_app_display    = "aad-${var.project_name}-${var.env}"
}

module "rg" {
  source   = "../../Azure/modules/resource-group"
  name     = local.rg_name
  location = var.location
  tags     = var.tags
}

module "logs" {
  source              = "../../Azure/modules/logs-insights"
  resource_group_name = module.rg.name
  location            = var.location
  log_name            = local.log_name
  appi_name           = local.appi_name
  tags                = var.tags
}

module "kv" {
  source                        = "../../Azure/modules/key-vault"
  name                          = local.kv_name
  resource_group_name           = module.rg.name
  location                      = var.location
  public_network_access_enabled = var.kv_public_network_access
  tags                          = var.tags
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
  log_analytics_ws_id = module.logs.log_analytics_workspace_id
  acr_id              = var.enable_acr && var.enable_aks ? module.acr[0].id : ""
  tags                = var.tags
}

module "service_plan" {
  source              = "../../Azure/modules/service-plan"
  name                = local.service_plan_name
  resource_group_name = module.rg.name
  location            = var.location
  sku                 = var.plan_sku
  tags                = var.tags
}

module "web" {
  source                         = "../../Azure/modules/app-service-web"
  name                           = local.web_name
  resource_group_name            = module.rg.name
  location                       = var.location
  service_plan_id                = module.service_plan.id
  dotnet_version                 = var.web_dotnet_version
  app_insights_connection_string = module.logs.app_insights_connection_string
  tags                           = var.tags
}

module "func_external" {
  source                         = "../../Azure/modules/function-app"
  name                           = local.func_external_name
  resource_group_name            = module.rg.name
  location                       = var.location
  service_plan_id                = module.service_plan.id
  runtime                        = var.function_external_runtime
  app_insights_connection_string = module.logs.app_insights_connection_string
  tags                           = var.tags
}

module "func_cron" {
  source                         = "../../Azure/modules/function-app"
  name                           = local.func_cron_name
  resource_group_name            = module.rg.name
  location                       = var.location
  service_plan_id                = module.service_plan.id
  runtime                        = var.function_cron_runtime
  app_insights_connection_string = module.logs.app_insights_connection_string
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

module "sql" {
  count                            = var.enable_sql ? 1 : 0
  source                           = "../../Azure/modules/sql-database"
  server_name                      = local.sql_server_name
  db_name                          = var.sql_db_name
  resource_group_name              = module.rg.name
  location                         = var.location
  admin_login                      = var.sql_admin_login
  admin_password                   = var.sql_admin_password
  public_network_access_enabled    = var.sql_public_network_access
  minimum_tls_version              = var.sql_minimum_tls_version
  sku_name                         = var.sql_sku_name
  auto_pause_delay_in_minutes      = var.sql_auto_pause_minutes
  max_size_gb                      = var.sql_max_size_gb
  firewall_rules                   = var.sql_firewall_rules
  tags                             = var.tags
}

module "aad_app" {
  count        = var.enable_aad_app ? 1 : 0
  source       = "../../Azure/modules/aad-app"
  display_name = local.aad_app_display
}

output "resource_group_name"        { value = module.rg.name }
output "acr_name"                   { value = var.enable_acr ? module.acr[0].name : null }
output "aks_name"                   { value = var.enable_aks ? module.aks[0].name : null }
output "web_app_name"               { value = module.web.name }
output "func_external_name"         { value = module.func_external.name }
output "func_cron_name"             { value = module.func_cron.name }
output "storage_data_account_name"  { value = var.enable_storage ? module.storage_data[0].name : null }
output "sql_server_name"            { value = var.enable_sql ? module.sql[0].server_name : null }
output "sql_database_id"            { value = var.enable_sql ? module.sql[0].database_id : null }
output "aad_app_client_id"          { value = var.enable_aad_app ? module.aad_app[0].client_id : null }
output "service_plan_id"            { value = module.service_plan.id }
