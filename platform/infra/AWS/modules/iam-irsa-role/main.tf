locals {
  issuer_hostpath = replace(var.cluster_oidc_issuer_url, "https://", "")
  role_name       = "${var.name_prefix}-irsa-${var.namespace}-${var.service_account_name}"
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
            "${local.issuer_hostpath}:aud" = "sts.amazonaws.com",
            "${local.issuer_hostpath}:sub" = "system:serviceaccount:${var.namespace}:${var.service_account_name}"
          }
        }
      }
    ]
  })
  tags = merge(var.tags, { Name = local.role_name })
}

# Optional policy for Secrets Manager read access
data "aws_iam_policy_document" "secrets_read" {
  count = length(var.allowed_secret_arns) > 0 ? 1 : 0
  statement {
    sid    = "SecretsManagerRead"
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue",
      "secretsmanager:DescribeSecret"
    ]
    resources = var.allowed_secret_arns
  }
}

resource "aws_iam_policy" "secrets_read" {
  count  = length(var.allowed_secret_arns) > 0 ? 1 : 0
  name   = "${var.name_prefix}-irsa-secrets-read"
  policy = data.aws_iam_policy_document.secrets_read[0].json
  tags   = var.tags
}

resource "aws_iam_role_policy_attachment" "secrets_read" {
  count      = length(var.allowed_secret_arns) > 0 ? 1 : 0
  role       = aws_iam_role.this.name
  policy_arn = aws_iam_policy.secrets_read[0].arn
}

# Optional policy for SSM Parameter Store read access
data "aws_iam_policy_document" "ssm_read" {
  count = length(var.allowed_parameter_arns) > 0 ? 1 : 0
  statement {
    sid    = "SSMParameterRead"
    effect = "Allow"
    actions = [
      "ssm:GetParameter",
      "ssm:GetParameters",
      "ssm:GetParameterHistory"
    ]
    resources = var.allowed_parameter_arns
  }
}

resource "aws_iam_policy" "ssm_read" {
  count  = length(var.allowed_parameter_arns) > 0 ? 1 : 0
  name   = "${var.name_prefix}-irsa-ssm-read"
  policy = data.aws_iam_policy_document.ssm_read[0].json
  tags   = var.tags
}

resource "aws_iam_role_policy_attachment" "ssm_read" {
  count      = length(var.allowed_parameter_arns) > 0 ? 1 : 0
  role       = aws_iam_role.this.name
  policy_arn = aws_iam_policy.ssm_read[0].arn
}

