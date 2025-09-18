module "docs_storage_account" {
  source = "../../../../new-terraform-modules/modules/data/storage_account"

  name                     = var.workflow_storage_account_docs
  resource_group_name      = module.resource_group.name
  location                 = module.resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  hns_enabled              = "true"
  sftp_enabled             = "false"
}

module "cron_storage_account" {
  source = "../../../../new-terraform-modules/modules/data/storage_account"

  name                     = var.workflow_storage_account_cron_function
  resource_group_name      = module.resource_group.name
  location                 = module.resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  hns_enabled              = "true"
  sftp_enabled             = "false"
}

module "external_storage_account" {
  source = "../../../../new-terraform-modules/modules/data/storage_account"

  name                     = var.workflow_storage_account_external_function
  resource_group_name      = module.resource_group.name
  location                 = module.resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  hns_enabled              = "true"
  sftp_enabled             = "false"
}
