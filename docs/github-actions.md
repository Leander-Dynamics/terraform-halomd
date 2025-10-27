# GitHub Actions for Terraform (Prod)

This repository includes a GitHub Actions workflow that mirrors the Azure DevOps (ADO) pipeline flow using self-contained steps (no local script dependencies):

- Validate → Plan → Guard → (optional) Apply
- Artifacts: plan JSON, plan summary (JSON/TXT), SQL-only plan summary (JSON/TXT)
- Guard blocks Apply when deletes/replaces > 0

## Workflow

File: `.github/workflows/terraform-prod.yml`

Trigger: manual `workflow_dispatch` with inputs:

- envName (default `prod`)
- runApply (default `false`)
- diagOnly (default `false`)
- applyDisableHub (default `true`) — blanks the hub provider subscription input so the hub alias is effectively disabled
- useAkv (default `true`) — reserved for future enhancement; current workflow doesn’t pull secrets from AKV during Plan

Jobs run on `ubuntu-latest` and perform Terraform inline (init/plan/show/apply). Plan and guard summaries use `jq` and PowerShell.

Artifacts uploaded under name `plan-<env>` include:

- `artifacts/plan-<env>.json`, `artifacts/plan-<env>.tfplan`, `artifacts/plan-<env>.log`
- `artifacts/plan-summary-<env>.json`, `artifacts/plan-summary-<env>.txt`
- `artifacts/<env>-plan-sql-*.json`, `artifacts/<env>-plan-sql-*.txt`

## Azure authentication

The workflow uses OIDC to log in to Azure via `azure/login@v2`. Configure the following repository secrets and federated credentials (recommended):

- `AZURE_CLIENT_ID` — App registration (federated credential for your repo)
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Follow: https://learn.microsoft.com/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#set-up-openid-connect

Alternatively, if you must use a service principal secret, set `azure/login` with `creds` JSON (not recommended). OIDC is preferred.

## Environment approvals (Apply)

The Apply job targets the `prod` environment. To require approvals:

1. In your GitHub repository → Settings → Environments → New environment → `prod`.
2. Add required reviewers or protection rules.
3. Re-run the workflow with `runApply=true`. The job will pause until approval.

## Key Vault and firewall notes

- Current workflow does not retrieve secrets from Key Vault during Plan. Plan uses variables from your env `terraform.tfvars` and TF_VAR_* values.
- You can add AKV retrieval later if needed; OIDC login is already in place. For now, keep `useAkv=false` or ignore it.

## Hub provider disabled

Set `applyDisableHub=true` to blank `TF_VAR_hub_subscription_id` at runtime so the hub `azurerm` alias is effectively disabled during Plan/Apply. Use this when you want Prod to behave like Dev (no hub permissions required).

## Try it

Actions → `Terraform Prod (Plan/Guard/Apply)` → Run workflow → Choose inputs:

- Start with: `envName=prod`, `runApply=false`, `diagOnly=false`, `applyDisableHub=true`.
- Review artifacts, ensure Guard shows `Deletes=0`, `Replaces=0`, and SQL-only changes = 0.
- When ready, re-run with `runApply=true` (Apply gated by the `prod` environment).

## Troubleshooting

- Plan JSON missing: check the Plan job logs to confirm Terraform ran and that the backend config exists.
- Guard fails due to deletes/replaces: review `plan-<env>.json` and `plan-summary-<env>.{json,txt}` to address drift; add `moved` blocks or `terraform import`, then re-plan.
- SQL-only summary missing: ensure PowerShell (`pwsh`) ran successfully; it is available on `ubuntu-latest`.
