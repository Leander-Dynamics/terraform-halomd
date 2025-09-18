resource "azurerm_redis_cache" "workflow-redis" {
  capacity                      = 1
  family                        = "C"
  location                      = azurerm_resource_group.workflow_rg.location
  name                          = "${var.env_region}-arbit-workflow-application-redis"
  public_network_access_enabled = false
  redis_version                 = "6"
  resource_group_name           = azurerm_resource_group.workflow_rg.name
  sku_name                      = "Standard"
}
