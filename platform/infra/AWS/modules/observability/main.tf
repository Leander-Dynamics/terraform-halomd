variable "name_prefix" { type = string }
variable "tags" { type = map(string) }

# Diagnostics S3 bucket (analog to Azure Storage diagnostics)
resource "aws_s3_bucket" "diagnostics" {
  bucket = "${var.name_prefix}-diag-${random_id.rand.hex}"
  tags   = merge(var.tags, { Name = "${var.name_prefix}-diagnostics" })
}

resource "random_id" "rand" {
  byte_length = 3
}

resource "aws_s3_bucket_versioning" "diag" {
  bucket = aws_s3_bucket.diagnostics.id
  versioning_configuration { status = "Enabled" }
}

resource "aws_s3_bucket_lifecycle_configuration" "diag" {
  bucket = aws_s3_bucket.diagnostics.id
  rule {
    id     = "glacier-365"
    status = "Enabled"
    # Apply to all objects
    filter { prefix = "" }
    transition {
      days          = 90
      storage_class = "STANDARD_IA"
    }
    transition {
      days          = 180
      storage_class = "GLACIER_IR"
    }
    expiration { days = 1095 }
  }
}

# Common application log group (apps can create their own)
resource "aws_cloudwatch_log_group" "apps" {
  name              = "/${var.name_prefix}/apps"
  retention_in_days = 30
  tags              = var.tags
}

output "diagnostics_bucket_name" { value = aws_s3_bucket.diagnostics.bucket }
output "apps_log_group" { value = aws_cloudwatch_log_group.apps.name }
