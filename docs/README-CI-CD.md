# ARBIT Terraform CI/CD on **Azure DevOps** — How the Pipeline Works

- **PR to `main`** → **Validate + Plan** (dev, stage, prod) — no applies.
- **Merge to `main`** → **Apply dev** automatically.
- **Stage/Prod** applies are **gated by Environment approvals**.
- **Secrets** are pulled **at runtime** from **Azure Key Vault** (AKV); nothing sensitive is stored in code.

See: `SECRETS-AKV.md` for the Key Vault flow.
