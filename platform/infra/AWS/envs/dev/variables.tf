variable "project_name" {
  type = string
}
variable "env_name" {
  type = string
}
variable "region" {
  type = string
}
variable "tags" {
  type = map(string)
}

# Remote state
variable "tf_state_bucket" {
  type = string
}
variable "tf_lock_table" {
  type = string
}
variable "tf_state_region" {
  type = string
}

# Networking
variable "vpc_cidr" {
  type = string
}
variable "az_count" {
  type    = number
  default = 3
}

# EKS
variable "eks_version" {
  type    = string
  default = "1.29"
}
variable "eks_node_instance_types" {
  type    = list(string)
  default = ["m5.large"]
}
variable "eks_desired_size" {
  type    = number
  default = 3
}
variable "eks_min_size" {
  type    = number
  default = 2
}
variable "eks_max_size" {
  type    = number
  default = 6
}

# RDS
variable "enable_rds" {
  type    = bool
  default = false
}
variable "rds_engine" {
  type    = string
  default = "aurora-postgresql"
} # or "sqlserver-ex"
variable "rds_instance_class" {
  type    = string
  default = "db.r6g.large"
}
variable "rds_username" {
  type    = string
  default = "dbadmin"
}
variable "rds_password" {
  type      = string
  sensitive = true
}

# Edge/WAF
variable "enable_cloudfront" {
  type    = bool
  default = false
}
variable "domain_name" {
  type    = string
  default = null
}
variable "hosted_zone_id" {
  type    = string
  default = null
}

# Optional: consume VPC from a separate remote state instead of creating a new one here
variable "use_remote_networking_state" {
  description = "When true, read VPC/subnets from an existing networking state in S3, instead of creating a new VPC."
  type        = bool
  default     = false
}

variable "networking_state_bucket" {
  description = "S3 bucket name that stores the networking Terraform state."
  type        = string
  default     = null
}

variable "networking_state_key" {
  description = "Key (path) to the networking Terraform state object in S3."
  type        = string
  default     = null
}

variable "networking_state_region" {
  description = "AWS region of the networking state S3 bucket."
  type        = string
  default     = null
}

# IRSA for app secrets/config access
variable "irsa_workloads" {
  description = "List of workload objects for IRSA secrets & parameter access (multi-workload)."
  type = list(object({
    name                        = string
    namespace                   = string
    service_account_name        = string
    secrets_manager_secret_arns = optional(list(string), [])
    ssm_parameter_prefixes      = optional(list(string), [])
  }))
  default = []
}
