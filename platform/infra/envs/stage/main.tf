locals {
  rg_name          = "rg-${var.project_name}-${var.env_name}"
  kv_name          = "kv-${var.project_name}-${var.env_name}"
  log_name         = "log-${var.project_name}-${var.env_name}"
  appi_name        = "appi-${var.project_name}-${var.env_name}"
  aad_app_display  = "aad-${var.project_name}-${var.env_name}"

  acr_name         = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name         = "aks-${var.project_name}-${var.env_name}-${var.location}"

  web_plan         = "asp-halomdweb-${var.env_name}-${var.location}"
  web_name         = "app-halomdweb-${var.env_name}"
  app_gateway_name = "agw-${var.project_name}-${var.env_name}"
  arbitration_plan = "asp-${var.project_name}-arb-${var.env_name}-${var.location}"
  arbitration_name = "app-${var.project_name}-arb-${var.env_name}"

  arbitration_plan_sku_effective        = var.arbitration_plan_sku != "" ? trimspace(var.arbitration_plan_sku) : "B1"
  arbitration_runtime_stack_effective   = var.arbitration_runtime_stack != "" ? trimspace(var.arbitration_runtime_stack) : "dotnet"
  arbitration_runtime_version_effective = var.arbitration_runtime_version != "" ? trimspace(var.arbitration_runtime_version) : "8.0"

  storage_data_name = lower(replace("st${var.project_name}${var.env_name}data", "-", ""))

  func_external_plan = "asp-external-${var.env_name}-${var.location}"
  func_external_name = "func-external-${var.env_name}"
  func_cron_plan     = "asp-cron-${var.env_name}-${var.location}"
  func_cron_name     = "func-cron-${var.env_name}"

  sql_server_name   = "sql-${var.project_name}-${var.env_name}"
  sql_database_name = var.sql_database_name != "" ? var.sql_database_name : "${var.project_name}-${var.env_name}"
  sql_admin_login_effective    = trimspace(coalesce(var.sql_admin_login, ""))
  sql_admin_password_effective = coalesce(var.sql_admin_password, "")
}

# Inject SQL admin password securely
locals {
  sql_admin_password_input = try(trim(var.sql_admin_password), "")
  resolved_sql_admin_password = local.sql_admin_password_input != "" ? local.sql_admin_password_input : (
    length(data.azurerm_key_vault_secret.sql_admin_password) > 0 ? data.azurerm_key_vault_secret.sql_admin_password[0].value : ""
  )
}

module "sql" {
  count                         = var.enable_sql && local.sql_admin_login_effective != "" && local.resolved_sql_admin_password != "" ? 1 : 0

  source                        = "../../Azure/modules/sql-serverless"
  server_name                   = local.sql_server_name
  database_name                 = local.sql_database_name
  resource_group_name           = module.resource_group.name
  location                      = var.location
  administrator_login           = local.sql_admin_login_effective
  administrator_password        = local.resolved_sql_admin_password
  public_network_access_enabled = var.sql_public_network_access
  minimum_tls_version           = var.sql_minimum_tls_version
  sku_name                      = var.sql_sku_name
  auto_pause_delay_in_minutes   = var.sql_auto_pause_delay
  max_size_gb                   = var.sql_max_size_gb
  min_capacity                  = var.sql_min_capacity
  max_capacity                  = var.sql_max_capacity
  read_scale                    = var.sql_read_scale
  zone_redundant                = var.sql_zone_redundant
  collation                     = var.sql_collation
  firewall_rules                = var.sql_firewall_rules
  tags                          = var.tags
}
