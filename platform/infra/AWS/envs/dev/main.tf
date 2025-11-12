locals {
  name_prefix = "${var.project_name}-${var.env_name}"
}

# Optional: read existing networking (VPC) from remote state
data "terraform_remote_state" "networking" {
  count   = var.use_remote_networking_state ? 1 : 0
  backend = "s3"
  config = {
    bucket  = var.networking_state_bucket
    key     = var.networking_state_key
    region  = var.networking_state_region
    encrypt = true
  }
}

module "vpc" {
  count  = var.use_remote_networking_state ? 0 : 1
  source = "../../modules/vpc"

  name_prefix = local.name_prefix
  vpc_cidr    = var.vpc_cidr
  az_count    = var.az_count
  tags        = var.tags
}

locals {
  effective_vpc_id             = var.use_remote_networking_state ? data.terraform_remote_state.networking[0].outputs.vpc_id : module.vpc[0].vpc_id
  effective_private_subnet_ids = var.use_remote_networking_state ? data.terraform_remote_state.networking[0].outputs.private_subnet_ids : module.vpc[0].private_subnet_ids
}

module "eks" {
  source = "../../modules/eks"

  name_prefix         = local.name_prefix
  vpc_id              = local.effective_vpc_id
  private_subnet_ids  = local.effective_private_subnet_ids
  eks_version         = var.eks_version
  node_instance_types = var.eks_node_instance_types
  desired_size        = var.eks_desired_size
  min_size            = var.eks_min_size
  max_size            = var.eks_max_size
  tags                = var.tags
}

module "iam_alb_controller" {
  source                  = "../../modules/iam-alb-controller"
  name_prefix             = local.name_prefix
  cluster_oidc_issuer_url = module.eks.cluster_oidc_issuer_url
  oidc_provider_arn       = module.eks.oidc_provider_arn
  namespace               = "kube-system"
  service_account_name    = "aws-load-balancer-controller"
  tags                    = var.tags
}

module "ecr" {
  source = "../../modules/ecr"

  name_prefix = local.name_prefix
  repos       = ["edr-agent", "fastapi-app", "flask-app", "python-webapp"]
  tags        = var.tags
}

module "rds" {
  count           = var.enable_rds ? 1 : 0
  source          = "../../modules/rds"
  name_prefix     = local.name_prefix
  vpc_id          = local.effective_vpc_id
  subnet_ids      = local.effective_private_subnet_ids
  engine          = var.rds_engine
  instance_class  = var.rds_instance_class
  master_username = var.rds_username
  master_password = var.rds_password
  tags            = var.tags
}

module "observability" {
  source      = "../../modules/observability"
  name_prefix = local.name_prefix
  tags        = var.tags
}

module "iam_irsa_secrets" {
  source                    = "../../modules/iam-irsa-secrets"
  cluster_oidc_issuer_url   = module.eks.cluster_oidc_issuer_url
  cluster_oidc_provider_arn = module.eks.oidc_provider_arn
  workloads                 = var.irsa_workloads
  tags                      = var.tags
}

module "edge" {
  count          = var.enable_cloudfront && var.domain_name != null && var.hosted_zone_id != null ? 1 : 0
  source         = "../../modules/edge"
  name_prefix    = local.name_prefix
  domain_name    = var.domain_name
  hosted_zone_id = var.hosted_zone_id
  tags           = var.tags
}
