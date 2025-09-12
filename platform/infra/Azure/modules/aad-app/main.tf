resource "azuread_application" "app" { display_name = var.display_name }
resource "azuread_service_principal" "sp" { application_id = azuread_application.app.application_id }
