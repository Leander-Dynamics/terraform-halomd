terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.33.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

locals {
  provider_subscription_id = try(trimspace(var.subscription_id), "") != "" ? var.subscription_id : null
  provider_tenant_id       = try(trimspace(var.tenant_id), "") != "" ? var.tenant_id : null
}

provider "azurerm" {
  features {}

  subscription_id = local.provider_subscription_id
  tenant_id       = local.provider_tenant_id
}

provider "azuread" {
  tenant_id = local.provider_tenant_id
}

provider "random" {}
