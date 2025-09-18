
# Define Network Security Group for VM
resource "azurerm_network_security_group" "arbit-workflow-application-ml-nsg" {
  name                = "${var.env_region}-arbit-workflow-application-ml-nsg-1"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name

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

  security_rule  {
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

# Define Network Interface
resource "azurerm_network_interface" "arbit-workflow-application-ml-nic" {
  count               = 2
  name                = "${var.env_region}-arbit-workflow-application-ml-nic-${count.index + 1}"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name

  ip_configuration {
    name                          = "workflow-ml-ipv4-config"
    subnet_id                     = "/subscriptions/${var.subscription_id}/resourceGroups/${var.vnet_resource_group}/providers/Microsoft.Network/virtualNetworks/${var.main_vnet}/subnets/${var.env_region}-private-applications-snet-1"
    private_ip_address_allocation = "Static"
    private_ip_address            = "${var.private_applications_subnet}.${28 + count.index}"
    primary                       = true
  }
}

resource "azurerm_network_interface_security_group_association" "workflow-ml-vm-nic-nsg-association" {
  count                     = 2
  network_interface_id      = azurerm_network_interface.arbit-workflow-application-ml-nic[count.index].id
  network_security_group_id = azurerm_network_security_group.arbit-workflow-application-ml-nsg.id
}

# Define Virtual Machine
resource "azurerm_linux_virtual_machine" "workflow" {
  count                = 2
  name                 = "${var.env_region}-arbit-workflow-application-ml-vm-${count.index + 1}"
  resource_group_name  = azurerm_resource_group.workflow_rg.name
  location             = azurerm_resource_group.workflow_rg.location
  size                 = "Standard_D2s_v4"
  admin_username       = "adminuser"
  network_interface_ids = [azurerm_network_interface.arbit-workflow-application-ml-nic[count.index].id]

  tags = {
    "Environment" = var.environment_label
    "Owner"       = "Krishna.Bhattarai@halomd.com"
  }

  admin_ssh_key {
    username   = "adminuser"
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
