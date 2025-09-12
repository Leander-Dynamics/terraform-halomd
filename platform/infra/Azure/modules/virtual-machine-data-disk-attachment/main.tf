
resource "azurerm_virtual_machine_data_disk_attachment" "this" {
  managed_disk_id    = var.disk_id
  virtual_machine_id = var.vm_id
  lun                = var.lun
  caching            = "ReadWrite"
}
