output "id" {
  value = azurerm_kubernetes_cluster.aks.id
}

output "aks_id" {
  description = "Resource ID of the AKS cluster"
  value       = azurerm_kubernetes_cluster.aks.id
}

output "kube_config" {
  value     = azurerm_kubernetes_cluster.aks.kube_config_raw
  sensitive = true
}

output "kubelet_identity_object_id" {
  description = "Object ID of the kubelet managed identity"
  value       = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
}

output "acr_id" {
  description = "Resource ID of the container registry"
  value       = azurerm_container_registry.acr.id
}

output "acr_login_server" {
  description = "ACR login server"
  value       = azurerm_container_registry.acr.login_server
}
