project_name    = "arbit"
env_name        = "dev"
location        = "eastus"
subscription_id = "930755b1-ef22-4721-a31a-1b6fbecf7da6"
tenant_id       = "70750cc4-6f21-4c27-bb0e-8b7e66bcb2dd"

tags = {
  project = "arbit"
  env     = "dev"
  owner   = "platform"
}

vnet_address_space = ["10.10.0.0/16"]
subnets = {
  gateway = {
    address_prefixes = ["10.10.0.0/24"]
  }
  web = {
    address_prefixes = ["10.10.1.0/24"]
  }
}
app_gateway_subnet_key  = "gateway"
app_gateway_fqdn_prefix = "agw-arbit-dev"
app_gateway_backend_fqdns = []

app_service_plan_sku    = "B1"
app_service_fqdn_prefix = "app-arbit-dev"
app_service_app_settings = {
  "WEBSITE_RUN_FROM_PACKAGE" = "0"
}
app_service_connection_strings = {
  PrimaryDatabase = {
    type  = "SQLAzure"
    value = "Server=tcp:sql-arbit-dev.database.windows.net,1433;Initial Catalog=halomd;User ID=sqladmin;Password=P@ssw0rd1234!;Encrypt=True;"
  }
}

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
    record = "app-arbit-dev.azurewebsites.net"
  }
}

sql_database_name        = "halomd"
sql_sku_name             = "GP_S_Gen5_2"
sql_max_size_gb          = 32
sql_auto_pause_delay     = 60
sql_min_capacity         = 0.5
sql_max_capacity         = 4
sql_admin_login          = "sqladmin"
sql_admin_password       = "P@ssw0rd1234!"
sql_firewall_rules = [
  {
    name             = "allow-all"
    start_ip_address = "0.0.0.0"
    end_ip_address   = "255.255.255.255"
  }
]
