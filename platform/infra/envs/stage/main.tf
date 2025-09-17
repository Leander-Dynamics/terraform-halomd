locals {
  rg_name          = "rg-${var.project_name}-${var.env_name}"
  kv_name          = "kv-${var.project_name}-${var.env_name}"
  log_name         = "log-${var.project_name}-${var.env_name}"
  appi_name        = "appi-${var.project_name}-${var.env_name}"
  aad_app_display  = "aad-${var.project_name}-${var.env_name}"

  acr_name = lower(replace("acr${var.project_name}${var.env_name}", "-", ""))
  aks_name = "aks-${var.project_name}-${var.env_name}-${var.location}"

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
  sql_admin_password_input     = try(trim(var.sql_admin_password), "")
  resolved_sql_admin_password = local.sql_admin_password_input != "" ? local.sql_admin_password_input : (
    length(data.azurerm_key_vault_secret.sql_admin_password) > 0 ? data.azurerm_key_vault_secret.sql_admin_password[0].value : ""
  )

  default_app_gateway_backend_fqdns = compact([
    module.app_service.default_hostname,
    module.app_service_arbitration.default_hostname,
  ])

  app_gateway_backend_fqdns = distinct(compact(concat(
    var.app_gateway_backend_fqdns,
    local.default_app_gateway_backend_fqdns,
  )))

  dns_hostname_overrides = {
    for hostname, replacement in {
      format("%s.azurewebsites.net", var.app_service_fqdn_prefix) = module.app_service.default_hostname
      format("%s.azurewebsites.net", local.arbitration_name)      = module.app_service_arbitration.default_hostname
    } : lower(hostname) => replacement
    if replacement != null && replacement != ""
  }

  dns_cname_records = {
    for name, cfg in var.dns_cname_records :
    name => merge(cfg, {
      record = lookup(local.dns_hostname_overrides, lower(cfg.record), cfg.record)
    })
  }
}
