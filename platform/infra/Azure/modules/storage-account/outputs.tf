output "name" { value = azurerm_storage_account.sa.name }
output "id" { value = azurerm_storage_account.sa.id }
output "primary_blob_endpoint" { value = azurerm_storage_account.sa.primary_blob_endpoint }
output "primary_access_key" {
  value     = azurerm_storage_account.sa.primary_access_key
  sensitive = true
}
output "primary_connection_string" {
  value     = azurerm_storage_account.sa.primary_connection_string
  sensitive = true
}
