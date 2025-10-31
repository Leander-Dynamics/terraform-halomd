locals {
  effective_name       = var.cluster_name != "" ? var.cluster_name : var.name
  effective_dns_prefix = var.dns_prefix != "" ? var.dns_prefix : local.effective_name
}

# Optional ACR co-located with AKS
resource "azurerm_container_registry" "acr" {
  name                = var.acr_name != "" ? var.acr_name : lower(replace(format("%sacr", local.effective_name), "_", ""))
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = var.acr_sku
  admin_enabled       = false
  tags                = var.tags
}

resource "azurerm_kubernetes_cluster" "aks" {
  name                = local.effective_name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = local.effective_dns_prefix

  sku_tier = "Free"

  default_node_pool {
    name                         = "system"
    vm_size                      = var.vm_size
    node_count                   = var.node_count
    os_disk_size_gb              = var.os_disk_size_gb
    type                         = "VirtualMachineScaleSets"
    orchestrator_version         = null
    only_critical_addons_enabled = false
    upgrade_settings {
      max_surge = "33%"
    }
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

  network_profile {
    network_plugin    = "azure" # Azure CNI
    network_policy    = "azure"
    outbound_type     = "loadBalancer"
    load_balancer_sku = "standard"
  }

  tags = var.tags
}

# Allow AKS kubelet to pull from ACR
resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
}
