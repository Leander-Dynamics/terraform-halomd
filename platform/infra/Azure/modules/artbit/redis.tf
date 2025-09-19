resource "azurerm_redis_cache" "workflow" {
  count                         = var.enable_redis ? 1 : 0
  name                          = format("%s-redis", local.workflow_suffix)
  location                      = var.region
  resource_group_name           = module.resource_group.name
  capacity                      = 1
  family                        = "C"
  sku_name                      = "Standard"
  redis_version                 = "6"
  public_network_access_enabled = false
  tags                          = var.tags
}
