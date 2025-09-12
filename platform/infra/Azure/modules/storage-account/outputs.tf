output "name"                      { value = azurerm_storage_account.sa.name }
output "primary_blob_endpoint"     { value = azurerm_storage_account.sa.primary_blob_endpoint }
output "primary_connection_string" {
  value     = azurerm_storage_account.sa.primary_connection_string
  sensitive = true
}
