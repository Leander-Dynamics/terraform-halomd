# GitHub Actions for Terraform (Prod)

This repository now includes a GitHub Actions workflow that mirrors the Azure DevOps (ADO) pipeline flow using the same scripts:

- Validate → Plan → Guard → (optional) Apply
- Artifacts: plan JSON, plan summary (JSON/TXT), SQL-only plan summary (JSON/TXT)
- Guard blocks Apply when deletes/replaces > 0

## Workflow

File: `.github/workflows/terraform-prod.yml`

Trigger: manual `workflow_dispatch` with inputs:

- envName (default `prod`)
- runApply (default `false`)
- diagOnly (default `false`)
- applyDisableHub (default `true`) — disables hub provider linkage and orphans hub-linked DNS from state
- useAkv (default `true`) — use Key Vault secrets during plan (may require firewall allowance)

Jobs run on `ubuntu-latest` and call the existing scripts:

- `.ado/scripts/tf-plan.sh`
- `.ado/scripts/tf-preapply.sh`
- `.ado/scripts/tf-apply.sh`
- `scripts/plan-summarize.sh`
- `scripts/plan-sql-check.ps1`
- `scripts/terraform-destroy-guard.sh`

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

- During Plan, if Key Vault firewall blocks access, you can set `useAkv=false` when dispatching to avoid KV interactions. The workflow will set `SKIP_KV_SECRETS=true` so Plan can complete.
- For Apply, ensure the agent can reach Key Vault or the scripts will manage firewall temporarily as per your existing logic.

## Hub provider disabled

Set `applyDisableHub=true` to force hub provider off and orphan hub-linked DNS from state. The plan script already honors `FORCE_DISABLE_HUB` and blanks `hub_subscription_id` accordingly.

## Try it

Actions → `Terraform Prod (Plan/Guard/Apply)` → Run workflow → Choose inputs:

- Start with: `envName=prod`, `runApply=false`, `diagOnly=false`, `applyDisableHub=true`, `useAkv=false` (safer plan)
- Review artifacts, ensure Guard shows `Deletes=0`, `Replaces=0`, and SQL-only changes = 0.
- When ready, re-run with `runApply=true` (Apply gated by the `prod` environment)

## Troubleshooting

- Plan JSON missing: check `.ado/scripts/tf-plan.sh` logs and ensure `terraform` installed.
- Guard fails due to deletes/replaces: review `plan-json-<env>` and `plan-summary-<env>` to address drift; use `moved` blocks or `terraform import`, then re-plan.
- SQL-only summary missing: ensure PowerShell (`pwsh`) is available (it is on ubuntu-latest). See `scripts/plan-sql-check.ps1`.
