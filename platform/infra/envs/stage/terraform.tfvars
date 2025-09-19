project_name = "arbit"
env_name     = "stage"

environment       = "stage"
environment_label = "Staging"
region            = "eastus2"
env_region        = "stage-eus2"
region_short      = "eus2"
ipv4_prefix       = "10.20.0"

subscription_id     = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
hub_subscription_id = "54b02500-d420-4838-a98a-00d0854b5592"
tenant_id           = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

vnet_resource_group = "stage-eus2-ops-rg-1"
main_vnet           = "stage-eus2-ops-vnet-1"

function_dns_zone_name           = "privatelink.azurewebsites.net"
function_dns_resource_group_name = "hub-eus2-vnet-rg-1"

vpns_ipv4 = [
  "10.0.0.0/24",
  "10.1.0.0/24",
  "10.2.0.0/24",
]

monitoring_ipv4 = "10.20.0.20/32"
octopus_ipv4    = "10.20.0.21/32"

private_applications_subnet = "10.20.5"

workflow_storage_account_docs              = "stageus2workflowdocs"
workflow_storage_account_cron_function     = "stageus2workflowcron"
workflow_storage_account_external_function = "stageus2workflowext"

workflow_sqlserver_dbadmin_password = null

ml_virtual_machine_count = 2

# Optional IP allowlists
briefbuilder_development_vdis = []
mpower_brief_avd_pool_ipv4    = []

tags = {
  project = "arbit"
  env     = "stage"
  owner   = "platform"
}

enable_key_vault_private_endpoint = true

vault_dns_zone_name = "privatelink.vaultcore.azure.net"

vault_dns_resource_group_name = "hub-eus2-vnet-rg-1"

enable_redis = false
