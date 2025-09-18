project_name = "arbit"
env_name     = "prod"

environment        = "prod"
environment_label  = "Production"
region             = "eastus2"
env_region         = "prod-eus2"
region_short       = "eus2"
ipv4_prefix        = "10.30.0"

subscription_id     = "40f3e169-b544-4789-936a-5526146e3b8e"
hub_subscription_id = "54b02500-d420-4838-a98a-00d0854b5592"
tenant_id           = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

vnet_resource_group = "prod-eus2-ops-rg-1"
main_vnet           = "prod-eus2-ops-vnet-1"

function_dns_zone_name           = "privatelink.azurewebsites.net"
function_dns_resource_group_name = "hub-eus2-vnet-rg-1"

azure_vpn_ipv4         = "10.0.0.0/24"
sonicwall_vpn_ipv4     = "10.1.0.0/24"
point_to_site_vpn_ipv4 = "10.2.0.0/24"
vpns_ipv4 = [
  "10.0.0.0/24",
  "10.1.0.0/24",
  "10.2.0.0/24",
]

monitoring_ipv4 = "10.30.0.20/32"
octopus_ipv4    = "10.30.0.21/32"

private_applications_subnet = "10.30.5"

workflow_storage_account_docs              = "prodeus2workflowdocs"
workflow_storage_account_cron_function     = "prodeus2workflowcron"
workflow_storage_account_external_function = "prodeus2workflowext"

workflow_sqlserver_dbadmin_password = null

ml_virtual_machine_count = 2

briefbuilder_development_vdis = []
halomd_development_test_vdi   = []
halomd_brief_avd_vnet_ipv4    = []
mpower_brief_avd_pool_ipv4    = []
vdis_ipv4                     = []

builder_ipv4 = ""
dagster_ipv4 = ""

public_operations_subnet      = ""
public_gateways_subnet        = ""
private_asps_subnet           = ""
private_gateways_subnet       = ""
private_services_subnet       = ""
private_powerplatform_subnet  = ""
private_psql_databases_subnet = ""
private_dataplatform_subnet   = ""
private_operations_subnet     = ""
public_mssql_databases_subnet = ""
private_databases_subnet      = ""

tags = {
  project = "arbit"
  env     = "prod"
  owner   = "platform"
}
