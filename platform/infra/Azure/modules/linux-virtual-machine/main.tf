resource "azurerm_linux_virtual_machine" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  # size                = var.size
  # admin_username      = var.admin_username

  network_interface_ids = [var.nic_id]
  tags                  = var.tags

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  # source_image_reference {
  #   publisher = var.image_publisher
  #   offer     = var.image_offer
  #   sku       = var.image_sku
  #   version   = "latest"
  # }
  #
  # admin_ssh_key {
  #   username   = var.admin_username
  #   public_key = var.ssh_key
  # }
}
