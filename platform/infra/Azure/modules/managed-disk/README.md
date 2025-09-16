# Managed Disk Module

Provisions an Azure managed disk resource.

## Inputs

- `name` (`string`, required) – Name of the managed disk.
- `location` (`string`, required) – Azure region where the managed disk will be deployed.
- `resource_group_name` (`string`, required) – Resource group in which the managed disk will be created.
- `disk_size_gb` (`number`, required) – Size of the managed disk in GB.
- `storage_account_type` (`string`, optional, default `Standard_LRS`) – Specifies the storage redundancy (for example `Standard_LRS` or `Premium_LRS`).
- `tags` (`map(string)`, optional, default `{}`) – Map of tags to apply to the managed disk.

## Example

```hcl
module "managed_disk" {
  source = "../modules/managed-disk"

  name                 = "data-disk-01"
  location             = azurerm_resource_group.example.location
  resource_group_name  = azurerm_resource_group.example.name
  disk_size_gb         = 128
  storage_account_type = "Premium_LRS"

  tags = {
    Environment = "dev"
    Project     = "example"
  }
}
