variable "role_name" { type = string }
variable "github_org" { type = string }
variable "github_repo" { type = string }
variable "policy_json" { type = string }

# OIDC provider for GitHub (one per account; often already exists)
data "aws_iam_openid_connect_provider" "github" {
  url = "https://token.actions.githubusercontent.com"
}

resource "aws_iam_role" "gha" {
  name = var.role_name
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow",
        Principal = { Federated = data.aws_iam_openid_connect_provider.github.arn },
        Action    = "sts:AssumeRoleWithWebIdentity",
        Condition = {
          StringLike = {
            "token.actions.githubusercontent.com:sub" = "repo:${var.github_org}/${var.github_repo}:*"
          },
          StringEquals = {
            "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
          }
        }
      }
    ]
  })
}

resource "aws_iam_policy" "gha" {
  name   = "${var.role_name}-policy"
  policy = var.policy_json
}

resource "aws_iam_role_policy_attachment" "gha" {
  role       = aws_iam_role.gha.name
  policy_arn = aws_iam_policy.gha.arn
}

output "role_arn" { value = aws_iam_role.gha.arn }
