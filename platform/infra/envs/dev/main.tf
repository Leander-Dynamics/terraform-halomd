# main.tf - Module declarations only (NO provider or required_providers block)

module "rg" {
  source   = "../../Azure/modules/resource-group"
  name     = "${var.project_name}-${var.env_name}-rg"
  location = var.location
  tags     = var.tags
}

module "acr" {
  count               = var.enable_acr ? 1 : 0
  source              = "../../Azure/modules/acr"
  name                = "${var.project_name}-${var.env_name}-acr"
  resource_group_name = module.rg.name
  location            = var.location
  sku                 = var.acr_sku
  tags                = var.tags
}

module "aks" {
  count               = var.enable_aks ? 1 : 0
  source              = "../../Azure/modules/aks"
  name                = "${var.project_name}-${var.env_name}-aks"
  resource_group_name = module.rg.name
  location            = var.location
  node_count          = var.aks_node_count
  vm_size             = var.aks_vm_size
  tags                = var.tags
}

module "sql" {
  count                         = var.enable_sql ? 1 : 0
  source                        = "../../Azure/modules/sql-database"
  server_name                   = "${var.project_name}-${var.env_name}-sqlsrv"
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
  display_name = "${var.project_name}-${var.env_name}-app"
}

module "kv" {
  source                        = "../../Azure/modules/key-vault"
  name                          = "${var.project_name}-${var.env_name}-kv"
  resource_group_name           = module.rg.name
  location                      = var.location
  public_network_access_enabled = var.kv_public_network_access
  tags                          = var.tags
}

module "logs" {
  source              = "../../Azure/modules/logs-insights"
  resource_group_name = var.app_insights_rg
  location            = var.location
  log_name            = "${var.project_name}-${var.env_name}-logs"
  appi_name           = var.app_insights_name
  tags                = var.tags
}

module "func_cron" {
  source                         = "../../Azure/modules/function-app"
  name                           = "${var.project_name}-${var.env_name}-func-cron"
  plan_name                      = "${var.project_name}-${var.env_name}-asp"
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.func_plan_sku
  runtime                        = var.function_cron_runtime
  app_insights_connection_string = var.app_insights_connection_string
  tags                           = var.tags
}

module "func_external" {
  source                         = "../../Azure/modules/function-app"
  name                           = "${var.project_name}-${var.env_name}-func-ext"
  plan_name                      = "${var.project_name}-${var.env_name}-asp"
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.func_plan_sku
  runtime                        = var.function_external_runtime
  app_insights_connection_string = var.app_insights_connection_string
  tags                           = var.tags
}

module "web" {
  source                         = "../../Azure/modules/app-service-web"
  name                           = "${var.project_name}-${var.env_name}-web"
  plan_name                      = "${var.project_name}-${var.env_name}-asp"
  resource_group_name            = module.rg.name
  location                       = var.location
  plan_sku                       = var.web_plan_sku
  dotnet_version                 = var.web_dotnet_version
  app_insights_connection_string = var.app_insights_connection_string
  tags                           = var.tags
}

module "storage_data" {
  count               = var.enable_storage ? 1 : 0
  source              = "../../Azure/modules/storage-account"
  name                = "${var.project_name}${var.env_name}data"
  resource_group_name = module.rg.name
  location            = var.location
  tags                = var.tags
}
