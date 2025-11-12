variable "project_name" { type = string }
variable "env_name" { type = string }
variable "region" { type = string }
variable "tags" { type = map(string) }

variable "tf_state_bucket" { type = string }
variable "tf_lock_table" { type = string }
variable "tf_state_region" { type = string }

variable "vpc_cidr" { type = string }
variable "az_count" {
  type    = number
  default = 3
}

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

variable "enable_rds" {
  type    = bool
  default = false
}
variable "rds_engine" {
  type    = string
  default = "aurora-postgresql"
}
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

variable "use_remote_networking_state" {
  type    = bool
  default = false
}
variable "networking_state_bucket" {
  type    = string
  default = null
}
variable "networking_state_key" {
  type    = string
  default = null
}
variable "networking_state_region" {
  type    = string
  default = null
}

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
