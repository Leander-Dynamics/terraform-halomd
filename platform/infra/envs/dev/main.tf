module "web_app" {
  source = "../../Azure/modules/web-app"

  name                = var.name
  app_name            = var.name
  plan_name           = var.plan_name
  plan_sku            = var.plan_sku
  resource_group_name = var.resource_group_name
  location            = var.location

  runtime_stack   = var.runtime_stack
  runtime_version = var.runtime_version
  always_on       = var.always_on
  https_only      = true
  ftps_state      = "Disabled"

  app_insights_connection_string = var.app_insights_connection_string
  log_analytics_workspace_id     = var.log_analytics_workspace_id
  run_from_package               = var.run_from_package
  app_settings                   = var.app_settings
  connection_strings             = var.connection_strings

  tags = var.tags
}
