module "docs_storage_account" {
  source = "../storage-account"

  name                = var.workflow_storage_account_docs
  resource_group_name = module.resource_group.name
  location            = var.region
  account_tier        = "Standard"
  replication_type    = "LRS"
  enable_hns          = true
}

module "cron_storage_account" {
  source = "../storage-account"

  name                = var.workflow_storage_account_cron_function
  resource_group_name = module.resource_group.name
  location            = var.region
  account_tier        = "Standard"
  replication_type    = "LRS"
  enable_hns          = true
}

module "external_storage_account" {
  source = "../storage-account"

  name                = var.workflow_storage_account_external_function
  resource_group_name = module.resource_group.name
  location            = var.region
  account_tier        = "Standard"
  replication_type    = "LRS"
  enable_hns          = true
}
