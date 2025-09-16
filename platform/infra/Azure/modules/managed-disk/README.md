# managed-disk module

Provisions an Azure managed disk resource.

## Example

```hcl
module "managed_disk" {
  source = "../modules/managed-disk"

  name                = "data-disk-01"
  location            = "eastus2"
  resource_group_name = azurerm_resource_group.example.name
  disk_size_gb        = 128

  tags = {
    Environment = "dev"
  }
}
```

The `tags` input is optional and lets you assign Azure resource tags to the disk. When omitted, the module defaults to an empty map.
