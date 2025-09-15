project_name    = "arbit"
env_name        = "prod"
location        = "eastus"
subscription_id = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id       = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

tags = {
  project = "arbit"
  owner   = "platform"
}

network_enabled       = true
network_address_space = ["10.30.0.0/16"]
network_subnets = {
  web = {
    address_prefixes = ["10.30.1.0/24"]
  }
  data = {
    address_prefixes = ["10.30.2.0/24"]
  }
  integration = {
    address_prefixes = ["10.30.3.0/24"]
  }
}

dns_enabled   = true
dns_zone_name = "az.halomd.com"
dns_a_records = {
  "api" = {
    ttl     = 3600
    records = ["10.30.1.10"]
  }
}
dns_cname_records = {
  "web" = {
    ttl   = 300
    record = "web-arbit-prod.azurewebsites.net"
  }
  "status" = {
    ttl   = 300
    record = "status.arbit.halomd.com"
  }
}

app_service_enabled                       = true
app_service_plan_sku                      = "P1v3"
app_service_dotnet_version                = "8.0"
app_service_app_settings                  = {
  "ASPNETCORE_ENVIRONMENT" = "Production"
  "WEBSITE_RUN_FROM_PACKAGE" = "1"
}
app_service_connection_strings = {
  DefaultConnection = {
    type  = "SQLAzure"
    value = "Server=tcp:sql-arbit-prod.database.windows.net,1433;Initial Catalog=arbit-prod;User ID=sqladmin;Password=P@ssw0rd1234!;Encrypt=True;"
  }
}
app_service_app_insights_connection_string = "InstrumentationKey=22222222-2222-2222-2222-222222222222;IngestionEndpoint=https://eastus-0.in.applicationinsights.azure.com/"
app_service_log_analytics_workspace_id     = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-monitor/providers/Microsoft.OperationalInsights/workspaces/log-arbit-prod"

sql_enabled                         = true
sql_admin_login                     = "sqladmin"
sql_admin_password                  = "P@ssw0rd1234!"
sql_public_network_access_enabled   = false
sql_sku_name                        = "GP_Gen5_4"
sql_auto_pause_delay_in_minutes     = 0
sql_max_size_gb                     = 512
sql_zone_redundant                  = true
sql_backup_storage_redundancy       = "Geo"
sql_firewall_rules = [
  {
    name             = "allow-azure"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "0.0.0.0"
  }
]
