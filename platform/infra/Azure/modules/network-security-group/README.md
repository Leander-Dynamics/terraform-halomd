# network-security-group module

Creates an Azure Network Security Group (NSG).

## Example

```hcl
module "nsg" {
  source = "../modules/network-security-group"

  name                = "nsg-example"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.example.name

  tags = {
    Environment = "test"
  }
}
```

The `tags` variable is optional; supply a map of tags when you want them applied to the NSG.
