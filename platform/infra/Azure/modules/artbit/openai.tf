resource "azurerm_cognitive_account" "openai" {
  name                = local.names.openai
  location            = var.region
  resource_group_name = module.resource_group.name
  kind                = "OpenAI"
  sku_name            = "S0"

  identity {
    type = "SystemAssigned"
  }

  tags = merge(var.tags, {
    environment = var.environment_label
    owner       = "Krishna Bhattarai"
  })
}
