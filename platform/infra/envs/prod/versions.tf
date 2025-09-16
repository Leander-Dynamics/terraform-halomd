terraform {
  required_version = ">= 1.3.0"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~>4.33" }
    azuread = { source = "hashicorp/azuread", version = "<latest>" }
    random  = { source = "hashicorp/random",  version = "<latest>" }
  }
}
