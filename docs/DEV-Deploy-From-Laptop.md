# Deploy Terraform to **Dev** from Your Work Laptop

Two ways:

- **Path A — Preferred:** *Laptop → PR → ADO auto‑applies Dev after merge.*
- **Path B — Direct CLI:** Local `terraform` to Dev (bootstrap/emergency only).

## Path A — Flow

```mermaid
flowchart TD
  subgraph Dev[Laptop]
    A1[Clone repo]
    A2[Create feature branch]
    A3[Edit dev vars<br/><code>platform/infra/envs/dev/terraform.tfvars</code>]
    A4[Commit & Push]
    A5[Open PR → main]
  end
  subgraph PRPipe[ADO PR Run]
    P1[Validate: fmt + init(-backend=false) + validate]
    P2[Plan dev → artifact <code>tfplan-dev</code>]
    P3[Plan stage/prod → artifacts]
  end
  subgraph MainPipe[ADO Main Run]
    M1[Validate]
    M2[Plan_All → artifacts]
    M3[Apply_Dev (auto) using <code>tfplan-dev</code>]
  end
  subgraph Azure[Azure]
    Z1[(Dev RG + KV + Logs/AppInsights + WebApp + Functions)]
  end
  A1-->A2-->A3-->A4-->A5-->P1-->P2-->P3-->M1-->M2-->M3-->Z1
```

## Path B — Local CLI to Dev

```mermaid
flowchart TD
  L1[Install: Terraform 1.7.5 + Azure CLI]
  L2[az login + select subscription]
  L3[export AZ\_SUBSCRIPTION\_ID/AZ\_TENANT\_ID]
  L4[cd platform/infra/envs/dev]
  L5[terraform init -reconfigure\n- backend-config=backend.tfvars\n- backend-config=\"subscription_id=$AZ_SUBSCRIPTION_ID\"\n- backend-config=\"tenant_id=$AZ_TENANT_ID\"]
  L6[terraform plan -var-file=terraform.tfvars -out tfplan-dev.tfplan]
  L7[terraform apply tfplan-dev.tfplan]
  L8[Verify in Azure]
  L1-->L2-->L3-->L4-->L5-->L6-->L7-->L8
```

### Supplying the backend subscription & tenant IDs securely

- **CI/CD:** Store `AZ_SUBSCRIPTION_ID` and `AZ_TENANT_ID` as secret pipeline variables (for example in the `terraform-<env>` variable group). The Azure CLI task exposes them as environment variables and the pipeline templates inject them into `terraform init` automatically.
- **Local runs:** Export the variables from your current Azure CLI context before running `terraform init`:

  ```bash
  export AZ_SUBSCRIPTION_ID=$(az account show --query id -o tsv)
  export AZ_TENANT_ID=$(az account show --query tenantId -o tsv)

  terraform init -reconfigure \
    -backend-config=backend.tfvars \
    -backend-config="subscription_id=${AZ_SUBSCRIPTION_ID}" \
    -backend-config="tenant_id=${AZ_TENANT_ID}"
  ```
