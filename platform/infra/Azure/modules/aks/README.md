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

  node_count = 2
  vm_size    = "Standard_DS2_v2"
  tags = {
    Environment = "example"
  }
}
```

The `dns_prefix` input is required and sets the prefix that Azure uses when creating DNS entries for the cluster's API server endpoint.
