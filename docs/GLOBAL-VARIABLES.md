# ADO Pipeline Global Variables (create in Azure DevOps)

> No secrets in code. Create these as Pipeline variables or Variable Groups.

## Global (non‑secret)
- `TF_VERSION` = `1.7.5`
- `TF_LOCK_TIMEOUT` = `20m`
- `USE_AKV_FOR_SECRETS` = `true` (pull secrets from Key Vault)
- `AKV_SECRET_SQL_ADMIN_LOGIN_NAME` = `sql-admin-login`
- `AKV_SECRET_SQL_ADMIN_PASSWORD_NAME` = `sql-admin-password`

## Per‑environment (non‑secret)
- `KV_NAME_DEV` = `kv-arbit-dev`
- `KV_NAME_QA` = `kv-arbit-qa`
- `KV_NAME_STAGE` = `kv-arbit-stage`
- `KV_NAME_PROD` = `kv-arbit-prod`

- `AZ_SUBSCRIPTION_ID` = `<subscription guid>`
- `AZ_TENANT_ID` = `<tenant guid>`

- `AKV_ENABLE_DYNAMIC_IP_DEV` = `true`   # temp IP allow during plan on hosted agents
- `AKV_ENABLE_DYNAMIC_IP_QA` = `true`
- `AKV_ENABLE_DYNAMIC_IP_STAGE` = `false`
- `AKV_ENABLE_DYNAMIC_IP_PROD` = `false`

## Optional (for Terraform‑managed RBAC on Key Vault)
- `KV_CICD_PRINCIPAL_ID_DEV` = `<objectId>` (fake default ok; update later)
- `KV_CICD_PRINCIPAL_ID_QA` = `<objectId>`
- `KV_CICD_PRINCIPAL_ID_STAGE` = `<objectId>`
- `KV_CICD_PRINCIPAL_ID_PROD` = `<objectId>`

> If you set these, Terraform can grant **Key Vault Secrets User** to the pipeline identity (uncomment the role assignment block in `platform/infra/envs/<env>/main.tf`).
