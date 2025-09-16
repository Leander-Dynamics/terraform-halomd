# Linux Virtual Machine Module

This module provisions an Azure Linux virtual machine using an existing network interface.  
Provide the VM sizing, administrator credentials, and image reference details via input variables.  
You can also optionally apply tags.

## Example Usage

```hcl
module "linux_vm" {
  source = "../../modules/linux-virtual-machine"

  name                = "example-vm"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.example.name
  nic_id              = azurerm_network_interface.example.id

  size           = "Standard_B2s"
  admin_username = "azureuser"

  image_publisher = "Canonical"
  image_offer     = "0001-com-ubuntu-server-jammy"
  image_sku       = "22_04-lts"

  ssh_key = file("~/.ssh/id_rsa.pub")

  tags = {
    Environment = "lab"
    Project     = "example"
  }
}
