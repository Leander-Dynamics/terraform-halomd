variable "cluster_oidc_provider_arn" {
  description = "ARN of the EKS cluster OIDC provider (aws_iam_openid_connect_provider ARN)."
  type        = string
}

variable "cluster_oidc_issuer_url" {
  description = "Issuer URL of the EKS cluster OIDC provider (e.g., https://oidc.eks.<region>.amazonaws.com/id/<id>)."
  type        = string
}

variable "workloads" {
  description = "List of workloads that require IRSA roles to read from Secrets Manager and/or SSM Parameter Store."
  type = list(object({
    name                        = string
    namespace                   = string
    service_account_name        = string
    secrets_manager_secret_arns = optional(list(string), [])
    ssm_parameter_prefixes      = optional(list(string), [])
  }))
  default = []
}

variable "tags" {
  description = "Common tags to apply to created IAM resources."
  type        = map(string)
  default     = {}
}
