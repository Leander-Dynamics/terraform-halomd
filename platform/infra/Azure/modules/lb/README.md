# lb module

Creates an Azure Load Balancer.

## Example

```hcl
module "lb" {
  source = "../modules/lb"

  name                = "lb-example"
  location            = "eastus2"
  resource_group_name = azurerm_resource_group.example.name
  public_ip_id        = azurerm_public_ip.example.id

  tags = {
    Environment = "dev"
  }
}
```

The `tags` input is optional and defaults to an empty map so tags are only set when you supply them.
