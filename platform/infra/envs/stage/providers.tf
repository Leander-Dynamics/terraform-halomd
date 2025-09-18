terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.33.0"
    }
  }
}

locals {
  subscription_id     = trimspace(coalesce(var.subscription_id, ""))
  tenant_id           = trimspace(coalesce(var.tenant_id, ""))
  hub_subscription_id = trimspace(coalesce(var.hub_subscription_id, ""))
}

provider "azurerm" {
  features {}

  subscription_id = local.subscription_id != "" ? local.subscription_id : null
  tenant_id       = local.tenant_id != "" ? local.tenant_id : null
}

provider "azurerm" {
  alias   = "hub"
  features {}

  subscription_id = local.hub_subscription_id != "" ? local.hub_subscription_id : null
  tenant_id       = local.tenant_id != "" ? local.tenant_id : null
}
