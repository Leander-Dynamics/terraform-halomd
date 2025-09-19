# Key Vault + Private Endpoint (Option A)
# - Creates a Key Vault scoped to the artbit workload RG
# - When enable_key_vault_private_endpoint = true:
#     * Disables public network access on the vault
#     * Creates a Private Endpoint in the 'services' subnet
#     * Joins the 'privatelink.vaultcore.azure.net' Private DNS zone (in hub/shared RG)

data "azurerm_client_config" "current" {}

# Prefer hub-shared KV DNS zone if provided; otherwise reuse function DNS RG
locals {
  kv_dns_rg = var.vault_dns_resource_group_name != "" ? var.vault_dns_resource_group_name : var.function_dns_resource_group_name
}

# Lookup the hub/shared Private DNS zone for Key Vault
data "azurerm_private_dns_zone" "kv_dns" {
  name                = var.vault_dns_zone_name
  resource_group_name = local.kv_dns_rg
  provider            = azurerm.hub
}

module "key_vault" {
  source  = "../key-vault"

  name                = "kv-${local.workflow_suffix}"
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  public_network_access_enabled = var.enable_key_vault_private_endpoint ? false : true
  enable_rbac_authorization     = true

  # Grant apps access via RBAC (Reader role can read secrets when combined with 'get' permission by policy;
  # if you use RBAC-only access to secrets, use built-in 'Key Vault Secrets User' role)
  rbac_assignments = {
    backend = {
      principal_id         = try(azurerm_linux_web_app.backend.identity[0].principal_id, null)
      role_definition_name = "Key Vault Secrets User"
    }
    frontend = {
      principal_id         = try(azurerm_linux_web_app.frontend.identity[0].principal_id, null)
      role_definition_name = "Key Vault Secrets User"
    }
  }

  tags = var.tags
}

# Private Endpoint for Key Vault (in services subnet)
resource "azurerm_private_endpoint" "kv" {
  count               = var.enable_key_vault_private_endpoint ? 1 : 0

  name                = "pep-${local.workflow_suffix}-kv-1"
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id           = local.subnet_ids.services

  private_service_connection {
    name                           = "kv-connection"
    is_manual_connection           = false
    private_connection_resource_id = module.key_vault.id
    subresource_names              = ["vault"]
  }

  private_dns_zone_group {
    name                 = "kv-dns"
    private_dns_zone_ids = [ data.azurerm_private_dns_zone.kv_dns.id ]
  }

  tags = var.tags
}
