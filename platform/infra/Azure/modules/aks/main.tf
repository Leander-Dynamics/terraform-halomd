resource "azurerm_kubernetes_cluster" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.dns_prefix

  default_node_pool {
    name       = "default"
    node_count = var.node_count
    vm_size    = var.vm_size
  }

  identity {
    type = "SystemAssigned"
  }

  dynamic "oms_agent" {
    for_each = var.log_analytics_workspace_id == null || var.log_analytics_workspace_id == "" ? [] : [var.log_analytics_workspace_id]
    content {
      log_analytics_workspace_id = oms_agent.value
    }
  }

  tags = var.tags
}
