resource "azurerm_private_endpoint" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = var.subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = var.private_service_connection.name
    private_connection_resource_id = var.private_service_connection.private_connection_resource_id
    subresource_names              = var.private_service_connection.subresource_names
    is_manual_connection           = var.private_service_connection.is_manual_connection
    request_message                = var.private_service_connection.request_message
  }

  dynamic "private_dns_zone_group" {
    for_each = var.private_dns_zone_groups

    content {
      name                 = private_dns_zone_group.value.name
      private_dns_zone_ids = private_dns_zone_group.value.private_dns_zone_ids
    }
  }
}
