variable "name_prefix" { type = string }
variable "repos" { type = list(string) }
variable "tags" { type = map(string) }

resource "aws_ecr_repository" "this" {
  for_each             = toset(var.repos)
  name                 = "${var.name_prefix}/${each.value}"
  image_tag_mutability = "MUTABLE"
  image_scanning_configuration { scan_on_push = true }
  tags = merge(var.tags, { Name = "${var.name_prefix}-${each.value}" })
}

output "repositories" {
  value = { for k, r in aws_ecr_repository.this : k => r.repository_url }
}
