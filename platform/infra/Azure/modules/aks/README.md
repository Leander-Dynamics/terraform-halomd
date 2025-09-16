# AKS module

Creates an Azure Kubernetes Service (AKS) cluster.

## Usage

```hcl
module "aks" {
  source = "../../Azure/modules/aks"

  name                = "aks-example"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  dns_prefix          = "aks-example"

  node_count    = 2
  vm_size       = "Standard_DS2_v2"
  identity_type = "SystemAssigned"
  tags = {
    Environment = "example"
  }
}
```

The `dns_prefix` input is required and sets the prefix that Azure uses when creating DNS entries for the cluster's API server endpoint.

Optional identity inputs allow configuring either a system-assigned or user-assigned managed identity for the cluster:

- `identity_type` defaults to `SystemAssigned` but also supports `UserAssigned` and `SystemAssigned,UserAssigned`.
- `identity_ids` supplies the list of user-assigned identity resource IDs when `identity_type` includes `UserAssigned`.
