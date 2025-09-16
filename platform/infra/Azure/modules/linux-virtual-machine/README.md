# linux-virtual-machine module

Creates an Azure Linux virtual machine with a supplied network interface.

## Example

```hcl
module "linux_vm" {
  source = "../modules/linux-virtual-machine"

  name                = "vm-example-01"
  location            = "eastus2"
  resource_group_name = azurerm_resource_group.example.name
  nic_id              = azurerm_network_interface.example.id

  tags = {
    Environment = "lab"
  }
}
```

`tags` is optional. If omitted the VM is created without any custom tags.
