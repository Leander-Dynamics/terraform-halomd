# Bastion module

Creates an Azure Bastion host, the required public IP address, and associates it with a dedicated `AzureBastionSubnet`.

## Usage

```hcl
module "bastion" {
  source = "../../Azure/modules/bastion"

  name                = "bas-example"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  subnet_id           = azurerm_subnet.bastion.id

  tags = {
    Environment = "example"
  }
}
```

## Defaults

* `sku` &rarr; `Standard`
* `scale_units` &rarr; `2` (applied only when using the Standard SKU)
* `copy_paste_enabled` &rarr; `true`
* `file_copy_enabled`, `ip_connect_enabled`, `shareable_link_enabled`, `tunneling_enabled` &rarr; `true` (automatically disabled when the Basic SKU is selected)
* `public_ip_allocation_method` &rarr; `Static`
* `public_ip_sku` &rarr; `Standard`
* `public_ip_name` defaults to `<name>-pip`
* `ip_configuration_name` &rarr; `default`
* `zones` &rarr; `[]`

A subnet named `AzureBastionSubnet` with a `/27` or larger CIDR is required. Provide the subnet ID to the module via the `subnet_id` input.
