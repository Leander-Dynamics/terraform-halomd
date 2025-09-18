resource "azurerm_resource_group" "workflow_rg" {
    name = "${var.env_region}-workflow-rg-1"
    location = var.region
}
