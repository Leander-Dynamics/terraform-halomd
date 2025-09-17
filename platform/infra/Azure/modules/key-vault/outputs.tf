output "id"        { value = azurerm_key_vault.kv.id }
output "name"      { value = azurerm_key_vault.kv.name }
output "vault_uri" { value = azurerm_key_vault.kv.vault_uri }

output "secret_ids" {
  description = "Map of secret resource IDs keyed by secret name."
  value       = { for name, secret in azurerm_key_vault_secret.this : name => secret.id }
}
