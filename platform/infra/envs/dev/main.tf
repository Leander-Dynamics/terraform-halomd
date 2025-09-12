
# main.tf - Module declarations only (NO provider or required_providers block)

module "rg" {
  source              = "../../Azure/modules/resource-group"
  name                = "${var.project_name}-${var.env_name}-rg"
  location            = var.location
}

module "acr" {
  source              = "../../Azure/modules/acr"
  name                = "${var.project_name}-${var.env_name}-acr"
  resource_group_name = var.deploy_rg
  location            = var.location
}

module "aks" {
  source              = "../../Azure/modules/aks"
  resource_group_name = var.deploy_rg
  location            = var.location
  name                = "${var.project_name}-${var.env_name}-aks"
}

module "sql" {
  source = "../../Azure/modules/sql-database"
}

module "aad_app" {
  source              = "../../Azure/modules/sql-database"
  resource_group_name = var.deploy_rg
  location            = var.location
  name                = "${var.project_name}-${var.env_name}-sql"
}

module "kv" {
  source = "../../Azure/modules/key-vault"
}

module "logs" {
  source = "../../Azure/modules/logs-insights"
}

module "func_cron" {
  source                         = "../../Azure/modules/function-app"
  name                           = "${var.project_name}-${var.env_name}-func-cron"
  plan_name                      = "${var.project_name}-${var.env_name}-asp"
  resource_group_name            = var.deploy_rg
  location                       = var.location
  app_insights_connection_string = var.app_insights_connection_string
}

module "func_external" {
  source                         = "../../Azure/modules/function-app"
  name                           = "${var.project_name}-${var.env_name}-func-ext"
  plan_name                      = "${var.project_name}-${var.env_name}-asp"
  resource_group_name            = var.deploy_rg
  location                       = var.location
  app_insights_connection_string = var.app_insights_connection_string
}

module "web" {
  source = "../../Azure/modules/app-service-web"
}

module "storage_data" {
  source = "../../Azure/modules/storage-account"
}

