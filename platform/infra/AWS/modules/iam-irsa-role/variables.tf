variable "name_prefix" { type = string }
variable "cluster_oidc_issuer_url" { type = string }
variable "oidc_provider_arn" { type = string }

variable "namespace" {
  description = "Kubernetes namespace where the ServiceAccount lives"
  type        = string
}

variable "service_account_name" {
  description = "Kubernetes ServiceAccount name to bind via IRSA"
  type        = string
}

variable "allowed_secret_arns" {
  description = "List of AWS Secrets Manager secret ARNs this role can read"
  type        = list(string)
  default     = []
}

variable "allowed_parameter_arns" {
  description = "List of AWS SSM Parameter Store ARNs this role can read"
  type        = list(string)
  default     = []
}

variable "tags" { type = map(string) }

