#!/usr/bin/env bash
set -euo pipefail
SUB=${1:?Subscription ID required}; RG=${2:-rg-tfstate-eastus}; LOC=${3:-eastus}; SA=${4:-stcodextfstate01}; CN_PREFIX=${5:-arbit}
az account set --subscription "$SUB"
az group create -n "$RG" -l "$LOC" -o none
az storage account create -g "$RG" -n "$SA" -l "$LOC" --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --allow-blob-public-access false -o none
KEY=$(az storage account keys list -g "$RG" -n "$SA" --query "[0].value" -o tsv)
for ENV in dev qa stage prod; do
  az storage container create --name "${CN_PREFIX}-${ENV}" --account-name "$SA" --account-key "$KEY" -o none
done
echo "backend.tfvars -> rg=$RG sa=$SA container=${CN_PREFIX}-<env> key=<env>-specific-state-key use_azuread_auth=true"
