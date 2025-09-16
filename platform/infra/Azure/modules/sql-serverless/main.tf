resource "azurerm_mssql_server" "this" {
  name                          = var.server_name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  version                       = "12.0"

  administrator_login           = var.administrator_login
  administrator_login_password  = var.administrator_password

  minimum_tls_version           = var.minimum_tls_version
  public_network_access_enabled = var.public_network_access_enabled

  tags = var.tags
}

resource "azurerm_mssql_database" "this" {
  name                        = var.database_name
  server_id                   = azurerm_mssql_server.this.id
  sku_name                    = var.sku_name
  collation                   = var.collation
  max_size_gb                 = var.max_size_gb
  auto_pause_delay_in_minutes = var.auto_pause_delay_in_minutes
  min_capacity                = var.min_capacity
  max_capacity                = var.max_capacity
  read_scale                  = var.read_scale
  zone_redundant              = var.zone_redundant
  backup_storage_redundancy   = var.backup_storage_redundancy

  tags = var.tags
}

resource "azurerm_mssql_firewall_rule" "this" {
  for_each         = { for rule in var.firewall_rules : rule.name => rule }
  name             = each.value.name
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = each.value.start_ip_address
  end_ip_address   = each.value.end_ip_address
}
