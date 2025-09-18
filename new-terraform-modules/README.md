# Shared Terraform Module Usage Guide

This repository shows how to consume **Terraform modules via SSH** from internal repositories, and how to maintain an automated **inventory of available modules**.  
It is designed to be used in **Azure DevOps** pipelines, but can work in any CI/CD system.

---
# Getting Started
1. Install terraform/opentofu
2. Install Make 
3. Bookmark [Azure Docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)

## 📦 Using Modules from an Internal Repository (SSH)

All modules must be pulled via SSH. Developers must configure their SSH keys in Azure DevOps or locally.

*Current Location* : git@ssh.dev.azure.com:v3/HaloMDLLC/Network%20Infrastructure/terraform-modules//modules/xxx?ref=vx.x.x
```hcl
module "storage_account" {
  source = "git@ssh.dev.azure.com:v3/<org>/<project>/<repo>//modules/storage_account?ref=v1.0.0"
  
# Example variables
  name                = "mystorageacct"
  resource_group_name = "rg-example"
  location            = "eastus"
  replication_type    = "LRS"
  access_tier         = "Hot"
}
```

# Contribute
The pipeline will do like 75%, just need to follow development practices.

``` bash
# First time setup
make init

# Check code quality
make lint

# Run terraform validate across modules
make validate

# Run tests (terraform test framework)
make test

# Generate/update changelog from commits since last tag
make changelog
```