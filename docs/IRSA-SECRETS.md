# AWS IRSA secrets access (Secrets Manager + SSM)

This repo provides a reusable Terraform module to create IRSA roles that let specific Kubernetes ServiceAccounts read:
- AWS Secrets Manager secrets (by explicit ARN)
- AWS Systems Manager Parameter Store parameters (by path prefix)

The module is wired into each AWS env and outputs ready-to-use ServiceAccount annotations for your Helm charts.

## Module

Module path: `platform/infra/AWS/modules/iam-irsa-secrets`

Inputs:
- `cluster_oidc_issuer_url` (string): EKS OIDC issuer URL (from the EKS module output)
- `cluster_oidc_provider_arn` (string): OIDC provider ARN (from the EKS module output)
- `workloads` (list of objects): one entry per workload ServiceAccount
  - `name` (string): identifier key for the workload (used in outputs/maps)
  - `namespace` (string): k8s namespace for the ServiceAccount
  - `service_account_name` (string): k8s ServiceAccount name
  - `secrets_manager_secret_arns` (list[string], optional): explicit Secrets Manager ARNs to allow Get/Describe
  - `ssm_parameter_prefixes` (list[string], optional): parameter path prefixes like `/app/web/` to allow read
- `tags` (map[string], optional)

Outputs:
- `workload_role_arns` (map[string]string): workload name -> IAM role ARN
- `service_account_annotations` (map[string]map[string]string): workload name -> annotation key/values

## Env wiring

Each env (`platform/infra/AWS/envs/{dev,qa,stage,prod}`) declares:

```
variable "irsa_workloads" {
  description = "List of workload objects for IRSA secrets & parameter access (multi-workload)."
  type = list(object({
    name                        = string
    namespace                   = string
    service_account_name        = string
    secrets_manager_secret_arns = optional(list(string), [])
    ssm_parameter_prefixes      = optional(list(string), [])
  }))
  default = []
}
```

And instantiates the module:

```
module "iam_irsa_secrets" {
  source                    = "../../modules/iam-irsa-secrets"
  cluster_oidc_issuer_url   = module.eks.cluster_oidc_issuer_url
  cluster_oidc_provider_arn = module.eks.oidc_provider_arn
  workloads                 = var.irsa_workloads
  tags                      = var.tags
}
```

Env outputs include:
- `irsa_workload_role_arns`
- `irsa_service_account_annotations`

## Example tfvars

```
irsa_workloads = [
  {
    name                   = "webapp"
    namespace              = "app"
    service_account_name   = "webapp"
    secrets_manager_secret_arns = [
      "arn:aws:secretsmanager:us-east-1:123456789012:secret:prod/webapp/db-abc123",
    ]
    ssm_parameter_prefixes = [
      "/prod/webapp/",
    ]
  },
  {
    name                   = "worker"
    namespace              = "app"
    service_account_name   = "worker"
    ssm_parameter_prefixes = [
      "/prod/worker/",
    ]
  }
]
```

## Helm usage

In your Helm values, set the ServiceAccount annotation with the role ARN. You can take it from Terraform outputs for the workload key.

Values snippet:

```
serviceAccount:
  create: true
  name: webapp
  annotations:
    eks.amazonaws.com/role-arn: <role-arn-from-tf-output>
```

If you prefer to generate the ServiceAccount via a manifest, ensure the same annotation is present.

## CI consumption (optional)

You can read the annotations map from Terraform outputs and inject into your Helm step.

PowerShell example (conceptual):

```powershell
$ann = terraform output -raw irsa_service_account_annotations | ConvertFrom-Json
$roleArn = $ann.webapp.'eks.amazonaws.com/role-arn'
# pass $roleArn to your Helm install/upgrade command as a value override
```

Notes:
- SSM permissions are granted by ARN pattern derived from `ssm_parameter_prefixes`.
- Use least-privilege: prefer narrow secret ARNs and tight parameter prefixes.
- IRSA trust binds exactly to `system:serviceaccount:<namespace>:<service_account_name>`.
