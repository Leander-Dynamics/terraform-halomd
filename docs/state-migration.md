# State Migration
- Point new root at the same backend (init -reconfigure).
- Use `terraform state mv` for module path refactors.
- Ensure a no-op plan before switching CI/CD.

## Moving state into per-environment containers

Each environment now stores Terraform state in its own container (`arbit-dev`, `arbit-stage`, `arbit-prod`). To migrate existing state from the legacy shared container:

1. In each environment directory run `terraform init -reconfigure -backend-config=backend.legacy.tfvars` (or temporarily restore the previous backend settings) and `terraform state pull > <env>.tfstate` to export the current state locally.
2. Update `backend.tfvars` to point at the new container (see `platform/infra/envs/<env>/backend.tfvars`).
3. Run `terraform init -reconfigure -backend-config=backend.tfvars` so Terraform writes the `.terraform` metadata for the new backend.
4. Push the saved state into the new container with `terraform state push <env>.tfstate` and rerun `terraform plan` to confirm a no-op.

> Tip: You can also copy the blob directly with `az storage blob copy start --destination-container arbit-<env> --destination-blob <state-key> --source-container arbit --source-blob <state-key>`, then rerun `terraform init -reconfigure` to verify.
