provider "azurerm" {
  alias           = "hub"
  features {}
  subscription_id = "54b02500-d420-4838-a98a-00d0854b5592"
}

data "azurerm_private_dns_zone" "function_dns" {
  name                = "privatelink.azurewebsites.net"
  resource_group_name = "hub-eus2-vnet-rg-1"
  provider            = azurerm.hub
}
