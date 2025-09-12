# Secrets & Credentials — Azure Key Vault (AKV) + ADO

## Goals
- **No secrets in code / repo / YAML.**
- Use **Azure Key Vault** for all credentials.
- Pipeline retrieves secrets **at runtime** via the **service connection** (OIDC).

## Two network patterns

### 1) Preferred — Private access (no public IP games)
- Put AKV behind a **Private Endpoint** in your VNet.
- Run the pipeline on a **self‑hosted agent** inside that VNet.
- In Key Vault, **public network access = false**; allow VNet/subnet only.
- Result: the agent reaches AKV privately; **no IP allow‑listing needed**.

### 2) Fallback — Public access with **temporary** IP allow‑listing (included in YAML)
- Keep `public_network_access_enabled = true`.
- Key Vault `network_acls.default_action = "Deny"`; allow **selected networks** only.
- The pipeline **detects the agent's public IP** and **temporarily adds** it to the AKV firewall **for the duration of the plan job**, then **removes** it.
- Works with **Microsoft‑hosted agents**.
- Trade‑off: brief exposure to a single ephemeral IP.

> You choose the pattern per environment. The YAML supports both. Set variables accordingly (see below).

## Required RBAC

Grant the **service principal** used by the ADO service connection the following **data-plane RBAC** at the Key Vault scope:
- **Key Vault Secrets User** (read secrets)  
Optional: **Key Vault Reader** (read metadata)

> You can also manage this in Terraform with a variable per env: `kv_cicd_principal_id` (objectId) and a role assignment.

## Pipeline variables to create in ADO (no secrets in code)

Create these **Pipeline variables** (Project → Pipelines → Library → Variable groups or per‑pipeline variables).

### Global (non‑secret)
- `TF_VERSION` = `1.7.5` (already in YAML; override if needed)
- `TF_LOCK_TIMEOUT` = `20m`
- `USE_AKV_FOR_SECRETS` = `true` | `false`  
  Whether to pull secrets from AKV at plan time.
- `AKV_SECRET_SQL_ADMIN_LOGIN_NAME` = `sql-admin-login`  
- `AKV_SECRET_SQL_ADMIN_PASSWORD_NAME` = `sql-admin-password`  

### Per environment (non‑secret)
- `KV_NAME_DEV` = `kv-arbit-dev`
- `KV_NAME_STAGE` = `kv-arbit-stage`
- `KV_NAME_PROD` = `kv-arbit-prod`

- `AKV_ENABLE_DYNAMIC_IP_DEV`   = `true`|`false`  
- `AKV_ENABLE_DYNAMIC_IP_STAGE` = `false` (recommended false for higher envs)
- `AKV_ENABLE_DYNAMIC_IP_PROD`  = `false` (recommended false for prod)

> If you implement **Private Endpoint + self‑hosted agent**, set `AKV_ENABLE_DYNAMIC_IP_* = false` and `public_network_access_enabled = false` in Terraform.

### Optional (for Terraform-managed RBAC)
- `KV_CICD_PRINCIPAL_ID_DEV` = `<object id of dev service connection principal>`
- `KV_CICD_PRINCIPAL_ID_STAGE` = `<object id ...>`
- `KV_CICD_PRINCIPAL_ID_PROD` = `<object id ...>`

Use these to create `azurerm_role_assignment` for **Key Vault Secrets User** at the vault scope (see the commented example in `envs/*/main.tf`).

## Secret names in AKV

Create these secrets **in each environment’s Key Vault** (names configurable via variables):
- `sql-admin-login`
- `sql-admin-password`

You can add more (e.g., `webapp-client-secret`) and extend the YAML similarly.

## How the pipeline uses AKV

During **Plan** jobs, the YAML:

1. **(Optional)** Detects the agent’s public IP and **temporarily** allows it in AKV firewall.
2. Reads secrets via `az keyvault secret show ...` using the **OIDC** service connection.
3. Passes secrets to Terraform via `-var` flags (never logged in shell due to `set -x` disabled around retrieval).
4. **Always removes** the temporary IP from the firewall (trap on exit).

> Apply stages **do not** need secrets because they use the saved plan artifact.

## Terraform inputs (no secrets committed)

In `platform/infra/envs/<env>/variables.tf` we define variables for SQL (and other creds). They are supplied only at **plan time** from AKV via the pipeline.

## Key Vault module — network controls

Our `key-vault` module supports:

```hcl
public_network_access_enabled = true|false
enable_rbac_authorization     = true

network_acls = {
  default_action             = "Allow"|"Deny"
  bypass                     = "AzureServices"|"None"
  ip_rules                   = ["x.x.x.x/32", ...]
  virtual_network_subnet_ids = [subnet_id, ...]
}
```

Use **RBAC** (recommended) rather than legacy access policies.


See **docs/GLOBAL-VARIABLES.md** for the exact list of pipeline variables to create.
