# Data block to get storage account ID
# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/storage_account_local_user
data "azurerm_storage_account" "target" {
  name                = var.storage_account_name
  resource_group_name = var.resource_group_name
}

# this may not be needed, based on auth
resource "azurerm_storage_account_local_user" "this" {
  name                 = "user1"
  storage_account_id   = azurerm_storage_account_target.id
  ssh_key_enabled      = true
  ssh_password_enabled = true
  home_directory       = "example_path"
  ssh_authorized_key {
    description = "key1"
    key         = local.first_public_key
  }
  ssh_authorized_key {
    description = "key2"
    key         = local.second_public_key
  }
  permission_scope {
    permissions {
      read   = true
      create = true
    }
    service       = "blob"
    resource_name = azurerm_storage_container.example.name
  }
}
