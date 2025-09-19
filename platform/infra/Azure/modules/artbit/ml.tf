locals {
  ml_vm_tags = merge(var.tags, {
    Environment = var.environment_label
    Owner       = "Krishna.Bhattarai@halomd.com"
  })
}

resource "azurerm_network_security_group" "ml" {
  name                = local.names.ml_nsg
  location            = var.region
  resource_group_name = module.resource_group.name
  tags                = var.tags

  security_rule {
    name                       = "allow-ssh-ipv4"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefixes    = concat(var.vpns_ipv4, [var.octopus_ipv4])
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "allow-prometheus"
    priority                   = 200
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "9100"
    source_address_prefix      = var.monitoring_ipv4
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "allow-https"
    priority                   = 300
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "443"
    source_address_prefixes    = concat(var.vpns_ipv4, var.briefbuilder_development_vdis, var.mpower_brief_avd_pool_ipv4)
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "deny-all-inbound"
    priority                   = 500
    direction                  = "Inbound"
    access                     = "Deny"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "*"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
}

resource "azurerm_network_interface" "ml" {
  count               = var.ml_virtual_machine_count
  name                = format("%s-%d", local.names.ml_nic, count.index + 1)
  location            = var.region
  resource_group_name = module.resource_group.name
  tags                = var.tags

  ip_configuration {
    name                          = "workflow-ml-ipv4-config"
    subnet_id                     = local.subnet_ids.applications
    private_ip_address_allocation = "Static"
    private_ip_address            = format("%s.%d", var.private_applications_subnet, 28 + count.index)
    primary                       = true
  }
}

resource "azurerm_network_interface_security_group_association" "ml" {
  count                     = var.ml_virtual_machine_count
  network_interface_id      = azurerm_network_interface.ml[count.index].id
  network_security_group_id = azurerm_network_security_group.ml.id
}

resource "azurerm_linux_virtual_machine" "ml" {
  count                 = var.ml_virtual_machine_count
  name                  = format("%s-%d", local.names.ml_vm, count.index + 1)
  resource_group_name   = module.resource_group.name
  location              = var.region
  size                  = var.ml_virtual_machine_size
  admin_username        = var.ml_virtual_machine_admin_username
  network_interface_ids = [azurerm_network_interface.ml[count.index].id]
  tags                  = local.ml_vm_tags

  admin_ssh_key {
    username   = var.ml_virtual_machine_admin_username
    public_key = file("${path.module}/public_keys/adminuser/id_ed25519.pub")
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "StandardSSD_LRS"
    disk_size_gb         = 64
  }

  source_image_reference {
    publisher = "canonical"
    offer     = "ubuntu-24_04-lts"
    sku       = "server"
    version   = "latest"
  }

  boot_diagnostics {}
}
