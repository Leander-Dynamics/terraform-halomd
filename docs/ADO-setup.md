# Azure DevOps (ADO) Setup

## Service Connections (OIDC)
Create four **Azure Resource Manager** service connections with **Workload identity federation**:
- `sc-azure-oidc-dev`
- `sc-azure-oidc-qa`
- `sc-azure-oidc-stage`
- `sc-azure-oidc-prod`

Grant **Contributor** on the target scope and **Storage Blob Data Contributor** on the Terraform state storage account used by your backends.

## Environments & Approvals
Create **Environments**: `dev`, `qa`, `stage`, `prod`.
- Add **Approvals** for `stage` and `prod` (SRE/lead/change approvers).
- (Optional) Add **Branch control** to allow only `refs/heads/main`.

## Branch policies
Protect `main`: require PR, build validation, reviewers, comment resolution.
