# artbit — Infrastructure (Option A, Infra-only, KV Private Endpoint enabled for Stage/Prod/QA)

This repository provisions **artbit** infrastructure for **dev**, **qa**, **stage**, and **prod** using Terraform.
Application code lives in a separate App repo. This repo is **Terraform only** and contains **no application sources**.

## Key decisions in Option A

- **No AKS/ACR** (removed).
- Single canonical modules root: `platform/infra/Azure/modules/**`.
- The workload module is **`platform/infra/Azure/modules/artbit`** (renamed from `arbit_workflow`).
- **Key Vault** is provisioned per environment.  
  - **Stage / Prod / QA**: **Private Endpoint + Private DNS** is **enabled** (PNA disabled).
  - **Dev**: KV Private Endpoint is **disabled by default** for speed; enable for full parity if desired.
- App Services are already **VNET-integrated** via `virtual_network_subnet_id` and can reach private endpoints.

> Toggle KV PE per env via `enable_key_vault_private_endpoint` in each `terraform.tfvars`.

---

## Layout

```
platform/
  infra/
    Azure/
      modules/
        artbit/              # main workload module
          kv_pe.tf           # Key Vault + PE + DNS (new)
          appservice.tf
          databases.tf
          functions.tf
          loadbalancer.tf
          storage.tf
          redis.tf
          ml.tf
          openai.tf
          main.tf
          variables.tf       # includes KV PE + External API toggles (new vars)
          outputs.tf
        resource-group/
        storage-account/
        # aks, acr removed
    envs/
      dev/
        main.tf              # module "artbit" with source = "../../Azure/modules/artbit"
        providers.tf         # includes azurerm and azurerm.hub (hub DNS lookups)
        variables.tf
        versions.tf
        backend.tfvars
        terraform.tfvars     # includes enable_key_vault_private_endpoint=false for Dev
      qa/                    # enable_key_vault_private_endpoint=true
      stage/                 # enable_key_vault_private_endpoint=true
      prod/                  # enable_key_vault_private_endpoint=true
azure-pipelines-infra-dev.yml   # Dev-only pipeline (uses variable groups)
azure-pipelines-infra-all.yml   # All-envs pipeline (uses variable groups per stage)
```

---

## Environment configuration

Each env folder has a `terraform.tfvars` with key values. We added:

```hcl
# KV PE toggle (ON for qa/stage/prod by default; OFF for dev by default)
enable_key_vault_private_endpoint = true|false

# Private DNS zone for Key Vault
vault_dns_zone_name              = "privatelink.vaultcore.azure.net"
vault_dns_resource_group_name    = "<your hub/private-dns RG>"
```

> The module will **look up** the Key Vault Private DNS zone in the hub RG and **attach** the Key Vault private endpoint to it.

---

## Pipelines (Azure DevOps) — use your Variable Groups

These YAMLs are ready to commit and use the variable group names exactly as shown in your screenshots:

- `terraform-global-common`: `TF_VERSION`, `TF_LOCK_TIMEOUT`, `PROJECT_NAME`, `LOCATION_DEFAULT`
- `terraform-dev|qa|stage|prod`: `AZ_SUBSCRIPTION_ID`, `AZ_TENANT_ID`, `DEPLOY_RG`, `ENV_NAME`, `LOCATION`, `SERVICE_CONNECTION`, `TF_BACKEND_*`, `TFSTATE_KV_NAME` (if you use it)

### Dev only

`azure-pipelines-infra-dev.yml` includes:

- `variables: [ terraform-global-common, terraform-dev ]`
- Fail-fast checks (no legacy module paths/names, no AKS/ACR)
- Runs `fmt`, `init`, `validate`, `plan`, `apply` in `platform/infra/envs/$(ENV_NAME)`

### All environments

`azure-pipelines-infra-all.yml` includes sequential stages:

- **Dev → QA → Stage → Prod**
- Each stage imports its **own variable group** (e.g., `terraform-qa`).
- Manual **validation gates** before **Stage** and **Prod** applies.

---

## CI usage (quick)

- In Azure DevOps ➜ Pipelines ➜ New pipeline ➜ YAML ➜ select `azure-pipelines-infra-dev.yml` or `azure-pipelines-infra-all.yml`.
- Ensure variable groups exist:
  - `terraform-global-common`
  - `terraform-dev`, `terraform-qa`, `terraform-stage`, `terraform-prod`
- Ensure the **service connections** match the `SERVICE_CONNECTION` values in the groups.

---

## Local usage (quick)

```bash
az login
az account set --subscription "<env AZ_SUBSCRIPTION_ID>"

cd platform/infra/envs/dev   # or qa/stage/prod
terraform init -backend-config=backend.tfvars
terraform validate
terraform plan -var-file=terraform.tfvars -out=tfplan
terraform apply -auto-approve tfplan
```

---

## Conflict-avoidance rules

- ✅ Only use modules under `platform/infra/Azure/modules/**`
- ❌ Remove/avoid `new-terraform-modules/**`
- ✅ Use module name **`artbit`** (not `arbit_workflow`)
- ❌ Do not re-add `aks/` or `acr/` modules
- ✅ Keep env `main.tf` source as `../../Azure/modules/artbit`
- Run `terraform fmt -recursive` before every PR

---

## Notes

- If you want the **QA** env to *not* use KV Private Endpoint, set `enable_key_vault_private_endpoint = false` in `platform/infra/envs/qa/terraform.tfvars`.
- If you never need the external function/API, keep `enable_external_api = false` (default).
- App pipelines (Repo B) should read secrets via **Key Vault references** or the appropriate app settings if you wire it. The infra here only provisions KV + PE + DNS; adding Key Vault refs to Web App settings can be done when you move secrets across.
