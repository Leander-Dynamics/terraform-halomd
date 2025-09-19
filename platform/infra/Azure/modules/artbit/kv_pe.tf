
# Key Vault and Private Endpoint (toggle by enable_key_vault_private_endpoint)
module "kv" {
  source                        = "../key-vault"
  name                          = local.names.key_vault
  resource_group_name           = module.resource_group.name
  location                      = var.region
  public_network_access_enabled = var.enable_key_vault_private_endpoint ? false : true
  enable_rbac_authorization     = true
  tags                          = var.tags
}

# Lookup shared private DNS zone for Key Vault (in hub RG)
data "azurerm_private_dns_zone" "kv" {
  name                = var.vault_dns_zone_name
  resource_group_name = var.vault_dns_resource_group_name
  provider            = azurerm.hub
}

resource "azurerm_private_endpoint" "kv" {
  count               = var.enable_key_vault_private_endpoint ? 1 : 0
  name                = format("pep-%s-kv-1", local.workflow_suffix)
  location            = var.region
  resource_group_name = module.resource_group.name
  subnet_id           = local.subnet_ids.services
  tags                = var.tags

  private_service_connection {
    name                           = format("%s-kv-psc", var.env_region)
    private_connection_resource_id = module.kv.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "kv"
    private_dns_zone_ids = [data.azurerm_private_dns_zone.kv.id]
  }
}
