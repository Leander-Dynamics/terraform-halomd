param(
  [Parameter(Mandatory=$true)] [string]$SubscriptionId,
  [string]$ResourceGroup = "rg-tfstate-eastus",
  [string]$Location      = "eastus",
  [string]$StorageAcct   = "stcodextfstate01",
  [string]$Container     = "tfstate"
)
az account set --subscription $SubscriptionId | Out-Null
az group create -n $ResourceGroup -l $Location -o none | Out-Null
az storage account create -g $ResourceGroup -n $StorageAcct -l $Location --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --allow-blob-public-access false -o none | Out-Null
$Key = az storage account keys list -g $ResourceGroup -n $StorageAcct --query "[0].value" -o tsv
az storage container create --name $Container --account-name $StorageAcct --account-key $Key -o none | Out-Null
Write-Host "backend.tfvars -> rg=$ResourceGroup sa=$StorageAcct container=$Container key=arbit/<env>.tfstate use_azuread_auth=true"
