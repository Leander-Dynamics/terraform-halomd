resource "azurerm_mssql_server" "server" {
  name                          = var.server_name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  version                       = "12.0"
  administrator_login           = var.admin_login
  administrator_login_password  = var.admin_password
  minimum_tls_version           = var.minimum_tls_version
  public_network_access_enabled = var.public_network_access_enabled
  tags                          = var.tags
}

resource "azurerm_mssql_database" "db" {
  name                        = var.db_name
  server_id                   = azurerm_mssql_server.server.id
  sku_name                    = var.sku_name
  max_size_gb                 = var.max_size_gb
  auto_pause_delay_in_minutes = var.auto_pause_delay_in_minutes
  read_scale                  = false
  tags                        = var.tags
}

resource "azurerm_mssql_firewall_rule" "rules" {
  for_each         = { for r in var.firewall_rules : r.name => r }
  name             = each.value.name
  server_id        = azurerm_mssql_server.server.id
  start_ip_address = each.value.start_ip
  end_ip_address   = each.value.end_ip
}
