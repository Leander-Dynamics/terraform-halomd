project_name    = "arbit"
env_name        = "dev"
location        = "eastus"
subscription_id = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id       = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

tags = {
  project = "arbit"
  owner   = "platform"
}

network_enabled       = true
network_address_space = ["10.10.0.0/16"]
network_subnets = {
  web = {
    address_prefixes = ["10.10.1.0/24"]
  }
  data = {
    address_prefixes = ["10.10.2.0/24"]
  }
}

dns_enabled   = true
dns_zone_name = "az.halomd.com"
dns_a_records = {
  "api-dev" = {
    ttl     = 3600
    records = ["10.10.1.10"]
  }
}
dns_cname_records = {
  "web-dev" = {
    ttl   = 3600
    record = "web-arbit-dev.azurewebsites.net"
  }
  "func-dev" = {
    ttl   = 3600
    record = "func-arbit-dev.azurewebsites.net"
  }
}

app_service_enabled                       = true
app_service_plan_sku                      = "B1"
app_service_dotnet_version                = "8.0"
app_service_app_settings                  = {
  "ASPNETCORE_ENVIRONMENT" = "Development"
}
app_service_connection_strings = {
  DefaultConnection = {
    type  = "SQLAzure"
    value = "Server=tcp:sql-arbit-dev.database.windows.net,1433;Initial Catalog=arbit-dev;User ID=sqladmin;Password=P@ssw0rd1234!;Encrypt=True;"
  }
}
app_service_app_insights_connection_string = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://eastus-0.in.applicationinsights.azure.com/"
app_service_log_analytics_workspace_id     = "/subscriptions/930755b1-ef22-4721-a31a-1b6fbecf7da6/resourceGroups/rg-monitor/providers/Microsoft.OperationalInsights/workspaces/log-arbit-dev"

sql_enabled                         = true
sql_admin_login                     = "sqladmin"
sql_admin_password                  = "P@ssw0rd1234!"
sql_public_network_access_enabled   = true
sql_sku_name                        = "GP_S_Gen5_2"
sql_auto_pause_delay_in_minutes     = 60
sql_max_size_gb                     = 75
sql_firewall_rules = [
  {
    name             = "allow-azure"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "0.0.0.0"
  }
]
