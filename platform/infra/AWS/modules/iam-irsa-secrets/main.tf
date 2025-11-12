locals {
  issuer_hostpath = trimsuffix(replace(var.cluster_oidc_issuer_url, "https://", ""), "/")
}

data "aws_iam_policy_document" "irsa_trust" {
  for_each = { for w in var.workloads : w.name => w }

  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [var.cluster_oidc_provider_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.issuer_hostpath}:sub"
      values   = ["system:serviceaccount:${each.value.namespace}:${each.value.service_account_name}"]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.issuer_hostpath}:aud"
      values   = ["sts.amazonaws.com"]
    }
  }
}

# Per-workload permissions: Secrets Manager and SSM Parameter Store (read-only)
data "aws_iam_policy_document" "permissions" {
  for_each = { for w in var.workloads : w.name => w }

  dynamic "statement" {
    for_each = length(each.value.secrets_manager_secret_arns) > 0 ? [1] : []
    content {
      sid    = "SecretsManagerRead"
      effect = "Allow"
      actions = [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ]
      resources = each.value.secrets_manager_secret_arns
    }
  }

  dynamic "statement" {
    for_each = length(each.value.ssm_parameter_prefixes) > 0 ? [1] : []
    content {
      sid    = "SSMParameterRead"
      effect = "Allow"
      actions = [
        "ssm:GetParameter",
        "ssm:GetParameters",
        "ssm:GetParameterHistory",
        "ssm:GetParametersByPath",
        "ssm:DescribeParameters"
      ]
      resources = [for p in each.value.ssm_parameter_prefixes : "arn:${data.aws_partition.current.partition}:ssm:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:parameter${p}*"]
    }
  }
}

data "aws_partition" "current" {}
data "aws_region" "current" {}
data "aws_caller_identity" "current" {}

resource "aws_iam_policy" "workload" {
  for_each    = data.aws_iam_policy_document.permissions
  name        = "irsa-${each.key}-secrets-read"
  description = "IRSA read-only access for ${each.key} to Secrets Manager/SSM"
  policy      = each.value.json
  tags        = var.tags
}

resource "aws_iam_role" "workload" {
  for_each           = data.aws_iam_policy_document.irsa_trust
  name               = "irsa-${each.key}-sa-role"
  assume_role_policy = each.value.json
  tags               = var.tags
}

resource "aws_iam_role_policy_attachment" "workload" {
  for_each   = aws_iam_policy.workload
  role       = aws_iam_role.workload[each.key].name
  policy_arn = each.value.arn
}
