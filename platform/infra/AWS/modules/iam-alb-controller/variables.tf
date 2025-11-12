variable "name_prefix" { type = string }
variable "cluster_oidc_issuer_url" { type = string }
variable "oidc_provider_arn" { type = string }
variable "namespace" {
  type    = string
  default = "kube-system"
}
variable "service_account_name" {
  type    = string
  default = "aws-load-balancer-controller"
}
variable "tags" { type = map(string) }
