output "role_arn" { value = aws_iam_role.this.arn }
output "sa_annotation_key" { value = "eks.amazonaws.com/role-arn" }
output "sa_annotation_value" { value = aws_iam_role.this.arn }

