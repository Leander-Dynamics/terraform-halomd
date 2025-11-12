locals {
  issuer_hostpath = replace(var.cluster_oidc_issuer_url, "https://", "")
  role_name       = "${var.name_prefix}-alb-controller-role"
}

resource "aws_iam_role" "this" {
  name = local.role_name
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow"
        Principal = { Federated = var.oidc_provider_arn }
        Action    = "sts:AssumeRoleWithWebIdentity"
        Condition = {
          StringEquals = {
            "${local.issuer_hostpath}:aud" = "sts.amazonaws.com"
            "${local.issuer_hostpath}:sub" = "system:serviceaccount:${var.namespace}:${var.service_account_name}"
          }
        }
      }
    ]
  })
  tags = merge(var.tags, { Name = local.role_name })
}

# Attach AWS managed policy for the AWS Load Balancer Controller
resource "aws_iam_role_policy_attachment" "alb_controller" {
  role       = aws_iam_role.this.name
  policy_arn = "arn:aws:iam::aws:policy/AWSLoadBalancerControllerIAMPolicy"
}
