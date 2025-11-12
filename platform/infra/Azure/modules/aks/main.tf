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
    auto_scaling_enabled         = var.enable_cluster_autoscaler
    min_count                    = var.enable_cluster_autoscaler ? var.min_count : null
    max_count                    = var.enable_cluster_autoscaler ? var.max_count : null
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

  // Optional Istio-based service mesh add-on
  dynamic "service_mesh_profile" {
    for_each = var.enable_istio_service_mesh ? [1] : []
    content {
      mode      = "Istio"
      revisions = []
    }
  }

  // Optional Application Gateway Ingress Controller (AGIC) integration
  dynamic "ingress_application_gateway" {
    for_each = var.enable_ingress_application_gateway && var.ingress_application_gateway_id != "" ? [1] : []
    content {
      gateway_id = var.ingress_application_gateway_id
    }
  }

  // Optional Cluster Autoscaler profile tuning
  dynamic "auto_scaler_profile" {
    for_each = var.enable_auto_scaler_profile ? [1] : []
    content {
      expander                     = var.auto_scaler_expander
      scan_interval                = var.auto_scaler_scan_interval
      balance_similar_node_groups  = var.auto_scaler_balance_similar_node_groups
      max_graceful_termination_sec = var.auto_scaler_max_graceful_termination_sec
    }
  }

  tags = var.tags
}

# (Planned) Optional user node pool with Cluster Autoscaler (provider v4 schema differs). To be added in follow-up.

# Allow AKS kubelet to pull from ACR
resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
}
