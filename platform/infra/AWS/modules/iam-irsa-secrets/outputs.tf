output "workload_role_arns" {
  description = "Map of workload name to created IAM role ARN."
  value       = { for k, v in aws_iam_role.workload : k => v.arn }
}

output "service_account_annotations" {
  description = "Map of workload name to Kubernetes serviceAccount annotation key/value pairs (for Helm chart values)."
  value = {
    for k, v in aws_iam_role.workload : k => {
      "eks.amazonaws.com/role-arn" = v.arn
    }
  }
}
