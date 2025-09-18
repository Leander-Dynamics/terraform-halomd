# Load Balancer 1 public IP
resource "azurerm_public_ip" "arbit_workflow_application_lb_public_ip_1" {
  name                = "${var.env_region}-arbit-workflow-application-lb-public-ip-1"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

# Load Balancer 1
resource "azurerm_lb" "arbit_workflow_application_lb_1" {
  name                = "${var.env_region}-arbit-workflow-application-lb-1"
  location            = azurerm_resource_group.workflow_rg.location
  resource_group_name = azurerm_resource_group.workflow_rg.name

  frontend_ip_configuration {
    name                 = "PublicIPAddress"
    public_ip_address_id = azurerm_public_ip.arbit_workflow_application_lb_public_ip_1.id
  }
}



# # The following may need to be adjusted a bit
# resource "azurerm_lb_backend_address_pool" "arbit_workflow_application_lb_backend_1" {
#   name                = "${var.env_region}-arbit-workflow-application-lb-backend-1"
#   loadbalancer_id     = azurerm_lb.arbit_workflow_application_lb_1.id
# }
#
# resource "azurerm_lb_probe" "arbit_workflow_application_lb_probe" {
#   name                = "${var.env_region}-arbit-workflow-application-lb-probe"
#   loadbalancer_id     = azurerm_lb.arbit_workflow_application_lb_1.id
#   protocol            = "Http"
#   port                = 80
#   request_path        = "/health"
#   number_of_probes    = 2
# }
#
# resource "azurerm_lb_rule" "arbit_workflow_application_lb_rule" {
#   name                           = "${var.env_region}-arbit-workflow-application-lb-rule"
#   loadbalancer_id                = azurerm_lb.arbit_workflow_application_lb_1.id
#   protocol                       = "Tcp"
#   frontend_port                  = 80
#   backend_port                   = 80
#   frontend_ip_configuration_name = "PublicIPAddress"
#   backend_address_pool_ids       = [azurerm_lb_backend_address_pool.arbit_workflow_application_lb_backend_1.id]
#   probe_id                       = azurerm_lb_probe.arbit_workflow_application_lb_probe.id
# }
