output "vpc_id" {
  value = var.use_remote_networking_state ? data.terraform_remote_state.networking[0].outputs.vpc_id : module.vpc[0].vpc_id
}
output "private_subnet_ids" {
  value = var.use_remote_networking_state ? data.terraform_remote_state.networking[0].outputs.private_subnet_ids : module.vpc[0].private_subnet_ids
}
output "eks_cluster_name" { value = module.eks.cluster_name }
output "eks_cluster_endpoint" { value = module.eks.cluster_endpoint }
output "alb_controller_role_arn" { value = module.iam_alb_controller.role_arn }
output "irsa_workload_role_arns" {
  value = try(module.iam_irsa_secrets.workload_role_arns, {})
}
output "irsa_service_account_annotations" {
  value = try(module.iam_irsa_secrets.service_account_annotations, {})
}
