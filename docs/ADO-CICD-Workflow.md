# ADO Terraform CI/CD — Full Workflow (Multi‑Stage)

See diagrams and steps in `DEV-Deploy-With-Branching.md` for branch context. This pipeline:
- PR → Validate + Plan (dev/stage/prod)
- Merge → Apply dev (auto)
- Stage/Prod → approvals

Key Vault integration is described in `SECRETS-AKV.md`.
