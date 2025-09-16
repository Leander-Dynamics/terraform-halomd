# Network Security Group Module

Creates an Azure Network Security Group (NSG).

## Usage

```hcl
module "network_security_group" {
  source = "../../Azure/modules/network-security-group"

  name                = "nsg-example"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location

  tags = {
    Environment = "test"
    Project     = "example"
  }
}
