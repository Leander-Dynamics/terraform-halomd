locals {
  admin_password_trimmed  = trimspace(coalesce(var.admin_password, ""))
  admin_password_provided = local.admin_password_trimmed != ""
  ssh_key_trimmed         = trimspace(coalesce(var.ssh_key, ""))
  ssh_key_provided        = local.ssh_key_trimmed != ""
}

resource "azurerm_linux_virtual_machine" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  size                = var.size
  admin_username      = var.admin_username
  admin_password      = local.admin_password_provided ? local.admin_password_trimmed : null
  disable_password_authentication = !local.admin_password_provided

  network_interface_ids = [var.nic_id]

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = var.image_publisher
    offer     = var.image_offer
    sku       = var.image_sku
    version   = var.image_version
  }

  dynamic "admin_ssh_key" {
    for_each = local.ssh_key_provided ? [local.ssh_key_trimmed] : []
    content {
      username   = var.admin_username
      public_key = admin_ssh_key.value
    }
  }

  lifecycle {
    precondition {
      condition     = local.ssh_key_provided || local.admin_password_provided
      error_message = "Either ssh_key or admin_password must be provided."
    }
  }
}
