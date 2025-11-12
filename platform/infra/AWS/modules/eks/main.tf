module "eks" {
  source  = "terraform-aws-modules/eks/aws"
  version = "~> 20.13"

  cluster_name    = "${var.name_prefix}-eks"
  cluster_version = var.eks_version

  vpc_id                         = var.vpc_id
  subnet_ids                     = var.private_subnet_ids
  enable_irsa                    = true
  cluster_endpoint_public_access = true

  eks_managed_node_groups = {
    default = {
      instance_types = var.node_instance_types
      min_size       = var.min_size
      max_size       = var.max_size
      desired_size   = var.desired_size
      tags           = var.tags
    }
  }

  tags = var.tags
}

output "cluster_name" { value = module.eks.cluster_name }
output "cluster_endpoint" { value = module.eks.cluster_endpoint }
output "cluster_certificate_authority_data" { value = module.eks.cluster_certificate_authority_data }
output "oidc_provider_arn" { value = module.eks.oidc_provider_arn }
output "oidc_provider" { value = module.eks.oidc_provider }
output "cluster_oidc_issuer_url" { value = module.eks.cluster_oidc_issuer_url }
