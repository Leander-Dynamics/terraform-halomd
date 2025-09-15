# ARBIT — Consolidated Infrastructure (Terraform) + Azure DevOps CI/CD

This repository contains:
- **Terraform modules** under `platform/infra/Azure/modules`.
- **Per-environment Terraform roots** under `platform/infra/envs/{dev,stage,prod}` with independent backends.
- **Azure DevOps CI/CD** under `.ado/` (reusable templates consumed by the multi-stage pipeline).
- **Bootstrap scripts** for Terraform state.
- **Docs** for setup, naming, and migration.

> **Posture**: AKS/ACR are present as modules but **disabled by default**. Optional **Storage** and **SQL** modules exist and are **off by default**. Key Vault public access is **toggleable**.

---

## Quick Start (ADO)

1. **Create service connections (OIDC)**: `sc-azure-oidc-dev`, `sc-azure-oidc-stage`, `sc-azure-oidc-prod`.
   - Scope to the target subscription/RG.
   - Also grant **Storage Blob Data Contributor** on the tfstate Storage Account.

2. **Create environments**: `dev`, `stage`, `prod` and add **approvals** for `stage` and `prod`.

3. **Verify backends**: in `platform/infra/envs/<env>/backend.tfvars` (prefilled) and **keep** `use_azuread_auth = true`.

4. **Run the pipeline**:
   - Use the root **`azure-pipelines.yml`** (multi-stage entry point that consumes `.ado/templates/*`).
   - **PR to main** → Validate & Plan (no apply).
   - **Merge to main** → Apply **dev**, then gated **stage**, then gated **prod**.

See `docs/ADO-setup.md` and `docs/step-by-step.md` for details.


Folder/File Structure:

arbit-consolidated-infra-ado/
├── azure-pipelines.yml                # Multi-stage pipeline (PR->Plan; merge->Apply dev -> gated stage/prod)
├── .ado/
│   └── templates/
│       ├── tf-validate.yml           # fmt + init/validate
│       ├── tf-plan.yml               # plan & publish plan artifact
│       └── tf-apply.yml              # apply the exact reviewed plan; uses ADO Environments
├── platform/
│   └── infra/
│       ├── Azure/
│       │   └── modules/
│       │       ├── aad-app/
│       │       ├── app-service-web/
│       │       ├── function-app/
│       │       ├── key-vault/
│       │       ├── logs-insights/
│       │       ├── resource-group/
│       │       ├── sql-database/
│       │       └── storage-account/
│       └── envs/
│           ├── dev/
│           │   ├── backend.tfvars     # prefilled: rg-tfstate-eastus/stcodextfstate01/tfstate; OIDC auth
│           │   ├── main.tf
│           │   ├── providers.tf
│           │   ├── terraform.tfvars   # AKS/ACR off; Storage/SQL toggles off by default
│           │   ├── variables.tf
│           │   └── versions.tf
│           ├── stage/ (same pattern)
│           └── prod/  (same pattern)
├── scripts/
│   ├── azure-bootstrap-tfstate.ps1
│   └── azure-bootstrap-tfstate.sh
├── docs/
│   ├── ADO-setup.md
│   ├── ci-cd-best-practices.md
│   ├── naming-conventions.md
│   ├── state-migration.md
│   ├── step-by-step.md
│   └── what-changed-and-why.md
├── .gitignore
└── README.md

