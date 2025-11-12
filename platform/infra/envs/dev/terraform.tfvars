project_name = "arbit"
env_name     = "dev"

environment       = "dev"
environment_label = "Development"
region            = "eastus2"
env_region        = "dev-eus2"
region_short      = "eus2"
ipv4_prefix       = "10.10.0"

subscription_id     = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
hub_subscription_id = "54b02500-d420-4838-a98a-00d0854b5592"
tenant_id           = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

vnet_resource_group = "dev-eus2-ops-rg-1"
main_vnet           = "dev-eus2-ops-vnet-1"

function_dns_zone_name           = "privatelink.azurewebsites.net"
function_dns_resource_group_name = "hub-eus2-vnet-rg-1"

vpns_ipv4 = [
  "10.0.0.0/24",
  "10.1.0.0/24",
  "10.2.0.0/24",
]

monitoring_ipv4 = "10.10.0.20/32"
octopus_ipv4    = "10.10.0.21/32"

private_applications_subnet = "10.10.5"

workflow_storage_account_docs              = "deveus2workflowdocs"
workflow_storage_account_cron_function     = "deveus2workflowcron"
workflow_storage_account_external_function = "deveus2workflowext"

workflow_sqlserver_dbadmin_password = null

ml_virtual_machine_count = 2

# Optional IP allowlists
briefbuilder_development_vdis = []
mpower_brief_avd_pool_ipv4    = []

# Secrets are injected at runtime via the pipeline / Key Vault where applicable.

tags = {
  project = "arbit"
  env     = "dev"
  owner   = "platform"
}

enable_key_vault_private_endpoint = false

vault_dns_zone_name = "privatelink.vaultcore.azure.net"

vault_dns_resource_group_name = "hub-eus2-vnet-rg-1"

enable_redis = false

# --- AKS and Ingress/Gateway/Front Door toggles for dev plan-only run ---
# Enable AKS cluster with Istio service mesh and AGIC via Application Gateway;
# also place Azure Front Door in front of the gateway.
enable_aks             = true
enable_aks_istio       = true
enable_app_gateway     = true
enable_aks_agw_ingress = true
enable_frontdoor       = true

# Cluster Autoscaler (planned wiring)
enable_aks_cluster_autoscaler = true
aks_min_count                 = 3
aks_max_count                 = 10

# Autoscaler profile tuning
enable_aks_auto_scaler_profile               = true
aks_auto_scaler_expander                     = "least-waste"
aks_auto_scaler_scan_interval                = "10s"
aks_auto_scaler_balance_similar_node_groups  = true
aks_auto_scaler_max_graceful_termination_sec = 600
