project_name = "arbit"
env_name     = "dev"
location     = "eastus"

# Secrets are injected at runtime via the pipeline / Key Vault.
# Ensure `sql_admin_login` and `sql_admin_password` are supplied securely (for example,
# via an untracked terraform.tfvars file, a variable group, or Key Vault) before
# running `terraform plan` or `terraform apply`.

tags = {
  project = "arbit"
  env     = "dev"
  owner   = "platform"
}

# -------------------------
# Networking
# -------------------------
vnet_address_space = ["10.10.0.0/16"]
subnets = {
  gateway = {
    address_prefixes = ["10.10.0.0/24"]
  }
  web = {
    address_prefixes = ["10.10.1.0/24"]
  }
}

kv_public_network_access = true
