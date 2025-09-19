resource "azurerm_redis_cache" "workflow" {
  name                          = local.names.redis
  location                      = module.resource_group.location
  resource_group_name           = module.resource_group.name
  capacity                      = 1
  family                        = "C"
  sku_name                      = "Standard"
  redis_version                 = "6"
  public_network_access_enabled = false
  tags                          = var.tags
}
