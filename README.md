# ARBIT — Consolidated Infrastructure (Terraform) + Azure DevOps CI/CD

ARBIT centralizes cloud infrastructure for development, staging, and production while automating deployments with Azure DevOps. This repository includes:

- **Terraform modules** under `platform/infra/Azure/modules` for reusable building blocks (resource groups, Key Vault, App Service, Functions, Storage, SQL, and optional AKS/ACR).
- **Per-environment Terraform roots** under `platform/infra/envs/{dev,stage,prod}` with independent remote state backends.
- **Azure DevOps (ADO) multi-stage pipeline** definitions under `azure-pipelines.yml` and `.ado/templates/*`.
- **Bootstrap scripts and documentation** that explain setup, networking posture, and ongoing operations (see `scripts/`, [`docs/`](docs/), and [`docs/network/`](docs/network/)).
- **Angular client developers**: see [`Arbitration/MPArbitration/ClientApp/README.md`](Arbitration/MPArbitration/ClientApp/README.md) for project-specific guidance.

> **Posture:** AKS/ACR modules exist but are **disabled by default**. Optional **Storage** and **SQL** modules are off by default, and Key Vault public network access is **toggleable per environment**.

---

## Overview

### Repository scope

- Manage infrastructure for three environments from a single codebase while keeping Terraform state isolated per environment.
- Enforce GitOps: changes flow through pull requests, plans are reviewed, and applies happen only from the main branch.
- Integrate security controls such as OIDC-based service connections and ADO Environment approvals for stage and prod.

### Pipeline behavior

- **Pull request to `main`** → Runs **Validate** and **Plan** for dev, stage, and prod (no applies).
- **Merge/push to `main`** → Runs **Validate**, **Plan**, **Apply dev**, then waits for approvals to **Apply stage** and **Apply prod**.
- Plans are published as pipeline artifacts and applies always use the previously reviewed `.tfplan` file.

---

## Prerequisites

### Quick start checklist

1. **Create ADO service connections (OIDC):** `sc-azure-oidc-dev`, `sc-azure-oidc-stage`, `sc-azure-oidc-prod` scoped to the target subscription/resource group with **Storage Blob Data Contributor** on the Terraform state storage account.
2. **Create ADO environments:** `dev`, `stage`, `prod`. Require approvals for stage and prod (optionally add branch control or business-hours policies).
3. **Verify remote state backends:** Each `platform/infra/envs/<env>/backend.tfvars` is prefilled; keep `use_azuread_auth = true` and update resource names if they differ in your tenant. Storage containers now use the pattern `arbit-<env>` so that each environment isolates its state blob.
4. **Import the pipeline:** Point Azure DevOps to the root `azure-pipelines.yml` multi-stage pipeline.
5. **Open a test pull request:** Review the three plan artifacts, merge to trigger the dev apply, and approve stage/prod when ready.

### Azure DevOps policies

- Require pull requests to update `main` (no direct pushes).
- Require build validation using this pipeline and enforce reviewer and comment resolution policies per your standards.
- Limit service connections so only this pipeline can use them.

### Recommended variables and secrets

- Global pipeline variables: `TF_VERSION = 1.7.5`, `TF_LOCK_TIMEOUT = 20m`, `USE_AKV_FOR_SECRETS = true`, `AKV_SECRET_SQL_ADMIN_LOGIN_NAME`, `AKV_SECRET_SQL_ADMIN_PASSWORD_NAME`.
- Per-environment variables: `KV_NAME_DEV|STAGE|PROD`, `AKV_ENABLE_DYNAMIC_IP_DEV|STAGE|PROD`.
- Optional: `KV_CICD_PRINCIPAL_ID_DEV|STAGE|PROD` if Terraform should grant Key Vault access to the pipeline principal.
- Store secrets in Azure Key Vault and retrieve them at runtime; no credentials are committed to the repo.

### Key Vault network patterns

- **Preferred:** Private endpoint + self-hosted agent → set `public network access = false`.
- **Fallback for hosted agents:** Keep public access enabled with firewall `default_action = "Deny"`; the pipeline temporarily allow-lists the agent IP when `AKV_ENABLE_DYNAMIC_IP_<env>` is true.

---

## Repository layout

```
arbit-consolidated-infra-ado/
├── azure-pipelines.yml                # Multi-stage pipeline entry point
├── .ado/
│   └── templates/
│       ├── tf-validate.yml            # fmt + init/validate
│       ├── tf-plan.yml                # plan & publish tfplan-<env>.tfplan
│       └── tf-apply.yml               # apply reviewed plan; uses ADO Environments
├── platform/
│   └── infra/
│       ├── Azure/
│       │   └── modules/               # Terraform modules (RG, KV, App Service, Functions, Storage, SQL, etc.)
│       └── envs/
│           ├── dev/                   # Terraform root with dedicated backend & tfvars
│           ├── stage/                 # Same structure as dev
│           └── prod/                  # Same structure as dev
├── scripts/
│   ├── azure-bootstrap-tfstate.ps1    # Bootstrap Terraform state resources (PowerShell)
│   └── azure-bootstrap-tfstate.sh     # Bootstrap Terraform state resources (Bash)
├── docs/
│   ├── ADO-setup.md                   # Detailed ADO onboarding
│   ├── step-by-step.md                # First-run walkthrough
│   ├── ADO-CICD-Workflow.md           # Pipeline diagrams and flows
│   ├── DEV-Deploy-From-Laptop.md      # Local Terraform workflow (optional)
│   ├── DEV-Deploy-With-Branching.md   # Branching guidance
│   ├── GLOBAL-VARIABLES.md            # Extended variable catalog
│   ├── SECRETS-AKV.md                 # Key Vault integration patterns
│   ├── ci-cd-best-practices.md        # Additional guardrails
│   ├── naming-conventions.md          # Resource naming standards
│   ├── network/                       # Network diagrams and per-environment notes (PDF/HTML/TXT)
│   ├── state-migration.md             # Migrating existing state
│   └── what-changed-and-why.md        # Rationale for the current structure
└── README.md                          # This document
```

---

## CI/CD pipeline

### Entry point and templates

- The pipeline lives in `azure-pipelines.yml` and composes the reusable YAML templates under `.ado/templates/*`.
- Jobs run purely in Bash via `AzureCLI@2`; no marketplace Terraform tasks are required.

### Stage breakdown

1. **Validate**
   - Installs Terraform (default `1.7.5`).
   - Runs `terraform fmt -check -recursive` on `platform/infra`.
   - Executes `init -backend=false` and `validate` against the dev root to catch schema issues early.
2. **Plan_All**
   - Parallel jobs for dev, stage, and prod.
   - Signs in with the environment-specific OIDC service connection.
   - Runs `terraform init -reconfigure -backend-config=backend.tfvars` and `terraform plan -var-file=terraform.tfvars -out tfplan-<env>.tfplan`.
   - Publishes each plan as a pipeline artifact (`tfplan-<env>`).
3. **Apply_Dev**
   - Triggers automatically on merges to `main` (never on PRs).
   - Downloads and applies the previously published `tfplan-dev.tfplan`.
4. **Apply_Stage**
   - Requires approval in the `stage` ADO Environment before running.
   - Applies the saved `tfplan-stage.tfplan`.
5. **Apply_Prod**
   - Requires approval in the `prod` ADO Environment before running.
   - Applies the saved `tfplan-prod.tfplan`.

### Integrity guardrails

- Apply stages only run when `Build.Reason != PullRequest` **and** `Build.SourceBranch == refs/heads/main`.
- Apply jobs consume the exact plan artifact produced earlier in the run, guaranteeing auditability.
- Service connections use workload identity federation (OIDC) to avoid client secrets.

### Promotion flow

1. Developer opens a PR → pipeline runs **Validate** and three **Plan** jobs.
2. Reviewers inspect the PR and the published plan artifacts; if approved, the PR merges to `main`.
3. Merge triggers dev apply automatically. Stage/prod applies wait for Environment approvals; approvers review and approve within Azure DevOps to continue the run.

### Configuration knobs

| Setting | Location | Notes |
| --- | --- | --- |
| Terraform version | `TF_VERSION` variable in YAML | Default `1.7.5`; update once for all jobs. |
| Lock timeout | `TF_LOCK_TIMEOUT` variable | Default `20m`; increase if state contention occurs. |
| Service connections | Parameters in plan/apply templates | Rename to match your ADO service connections. |
| Feature toggles | `platform/infra/envs/<env>/terraform.tfvars` | Toggle modules (Storage, SQL, AKS/ACR, Key Vault access). |
| Backend coordinates | `platform/infra/envs/<env>/backend.tfvars` | Update RG/Storage/Container names per environment (containers follow `arbit-<env>`). |
| SQL admin credentials | ADO variable groups / Key Vault / secure `terraform.tfvars` | Required when `enable_sql = true`; provide before running plan/apply. |

### Manual runs and re-runs

- **Run pipeline on `main`:** behaves like a merge (dev auto-applies, stage/prod await approvals).
- **Re-run failed stages:** Apply stages reuse the saved plan artifact to maintain integrity.
- **Redeploy from Environments:** Trigger a redeploy of the last successful run for a given environment using the stored artifact.

### Local Terraform helper script

- Use [`scripts/run-terraform.sh`](scripts/run-terraform.sh) to execute a one-shot `terraform init` + `terraform apply` for a specific environment.
- Syntax: `./scripts/run-terraform.sh <dev|qa|stage|prod>` (the script validates the environment and requires `terraform` in your `PATH`).
- Run the script from any directory; it automatically changes to `platform/infra/envs/<env>`, loads `backend.tfvars`, and applies using `terraform.tfvars` with `-auto-approve`.

### Security and governance

- Enforce least privilege by scoping service connections to the minimal subscription/resource group.
- Require PR reviews, build validation, and environment approvals to promote infrastructure changes.
- Store secrets in Key Vault or variable groups; never commit them to the repository.
- Keep an audit trail through plan artifacts, deployment logs, and environment approval history.

### Troubleshooting quick reference

| Symptom | Likely cause | Resolution |
| --- | --- | --- |
| `A required approval is pending` | Environment approval not granted | Approver must approve the `stage`/`prod` environment in Azure DevOps. |
| `Insufficient privileges to access the Storage Account` | Service connection missing **Storage Blob Data Contributor** | Grant the role to the service principal used by the service connection. |
| `Error acquiring the state lock` | Parallel run or stale lock | Wait/retry; optionally raise `TF_LOCK_TIMEOUT`. |
| `Backend not found` | Incorrect names in `backend.tfvars` | Update resource group/storage/container values to match reality. |
| Provider version errors | Agent using wrong Terraform/provider versions | Keep `TF_VERSION` pinned; rerun with `terraform init -reconfigure`. |
| OIDC login fails | Federated credential misconfigured | Recreate the service connection with workload identity federation and correct project/repo mapping. |

---

## References

- [docs/ADO-setup.md](docs/ADO-setup.md) — Detailed Azure DevOps onboarding (service connections, environments, branch policies).
- [docs/step-by-step.md](docs/step-by-step.md) — Guided first-run walkthrough from cloning to initial deployment.
- [docs/ADO-CICD-Workflow.md](docs/ADO-CICD-Workflow.md) — Visual diagrams of the PR and main pipeline flows.
- [docs/GLOBAL-VARIABLES.md](docs/GLOBAL-VARIABLES.md) — Extended catalog of suggested pipeline variables.
- [docs/SECRETS-AKV.md](docs/SECRETS-AKV.md) — Key Vault integration patterns and networking options.
- [docs/DEV-Deploy-From-Laptop.md](docs/DEV-Deploy-From-Laptop.md) & [docs/DEV-Deploy-With-Branching.md](docs/DEV-Deploy-With-Branching.md) — Developer workflows and branching strategy.
- [docs/ci-cd-best-practices.md](docs/ci-cd-best-practices.md) — Additional guardrails and recommendations.
- [docs/state-migration.md](docs/state-migration.md) — Migrating existing Terraform state into the consolidated structure.
- [docs/what-changed-and-why.md](docs/what-changed-and-why.md) — Rationale behind the current architecture.
- [docs/naming-conventions.md](docs/naming-conventions.md) — Resource naming standards used across environments.
- [docs/network/](docs/network/) — Network topology PDFs, HTML exports, and per-environment resource inventories.

