terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.33"
    }
  }
}

locals {
  base_name        = "${var.project_name}-${var.env_name}"
  workflow_suffix  = "${local.base_name}-workflow"
  regional_suffix  = "${var.region_short}-${local.workflow_suffix}"
  resource_group   = format("rg-%s", local.workflow_suffix)

  names = {
    backend_plan       = format("asp-%s-backend-1", local.workflow_suffix)
    backend_app        = format("web-%s-backend", local.workflow_suffix)
    backend_endpoint   = format("pep-%s-backend-1", local.workflow_suffix)
    frontend_plan      = format("asp-%s-frontend-1", local.workflow_suffix)
    frontend_app       = format("web-%s-frontend", local.workflow_suffix)
    frontend_endpoint  = format("pep-%s-frontend-1", local.workflow_suffix)
    cron_plan          = format("asp-%s-cron-1", local.workflow_suffix)
    cron_app           = format("func-%s-cron", local.workflow_suffix)
    cron_endpoint      = format("pep-%s-cron-1", local.workflow_suffix)
    external_plan      = format("asp-%s-external-1", local.workflow_suffix)
    external_app       = format("func-%s-external", local.workflow_suffix)
    external_nsg       = format("nsg-%s-external", local.workflow_suffix)
    redis              = format("%s-redis", local.workflow_suffix)
    sql_server         = format("sql-%s", local.workflow_suffix)
    sql_app_db         = format("%s-app-db", local.workflow_suffix)
    sql_logs_db        = format("%s-logs-db", local.workflow_suffix)
    load_balancer      = format("lb-%s-1", local.workflow_suffix)
    load_balancer_pip  = format("pip-%s-lb-1", local.workflow_suffix)
    load_balancer_pool = format("lbpool-%s-1", local.workflow_suffix)
    ml_nsg             = format("nsg-%s-ml", local.workflow_suffix)
    ml_nic             = format("nic-%s-ml", local.workflow_suffix)
    ml_vm              = format("vm-%s-ml", local.workflow_suffix)
    backend_insights   = format("appi-%s-backend", local.workflow_suffix)
    frontend_insights  = format("appi-%s-frontend", local.workflow_suffix)
    cron_insights      = format("appi-%s-cron", local.workflow_suffix)
    external_insights  = format("appi-%s-external", local.workflow_suffix)
    openai             = format("%s-openai", local.workflow_suffix)
  }

  subnet_names = {
    app_services = format("%s-private-asps-snet-1", var.env_region)
    services     = format("%s-private-services-snet-1", var.env_region)
    applications = format("%s-private-applications-snet-1", var.env_region)
  }

  subnet_ids = {
    app_services = format(
      "/subscriptions/%s/resourceGroups/%s/providers/Microsoft.Network/virtualNetworks/%s/subnets/%s",
      var.subscription_id,
      var.vnet_resource_group,
      var.main_vnet,
      local.subnet_names.app_services,
    )
    services = format(
      "/subscriptions/%s/resourceGroups/%s/providers/Microsoft.Network/virtualNetworks/%s/subnets/%s",
      var.subscription_id,
      var.vnet_resource_group,
      var.main_vnet,
      local.subnet_names.services,
    )
    applications = format(
      "/subscriptions/%s/resourceGroups/%s/providers/Microsoft.Network/virtualNetworks/%s/subnets/%s",
      var.subscription_id,
      var.vnet_resource_group,
      var.main_vnet,
      local.subnet_names.applications,
    )
  }
}

module "resource_group" {
  source   = "../resource-group"
  name     = local.resource_group
  location = var.region
}

data "azurerm_resource_group" "workflow" {
  name       = module.resource_group.name
  depends_on = [module.resource_group]
}

data "azurerm_private_dns_zone" "function_dns" {
  name                = var.function_dns_zone_name
  resource_group_name = var.function_dns_resource_group_name
  provider            = azurerm.hub
}
    key_vault         = format("kv-%s", local.workflow_suffix)
