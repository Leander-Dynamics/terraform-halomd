resource "azurerm_cognitive_account" "openai" {
  name                = "${var.env_region}-arbit-workflow-application-openai"  # Must be globally unique
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  kind                = "OpenAI"
  sku_name            = "S0"

  identity {
    type = "SystemAssigned"
  }

  tags = {
    environment = var.environment_label
    owner       = "Krishna Bhattarai"
  }
}

output "openai_endpoint" {
  value = azurerm_cognitive_account.openai.endpoint
}

output "openai_primary_key" {
  value     = azurerm_cognitive_account.openai.primary_access_key
  sensitive = true
}