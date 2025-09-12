output "application_id" { value = azuread_application.app.application_id }
output "client_id"      { value = azuread_application.app.client_id }
output "object_id"      { value = azuread_application.app.object_id }
output "sp_object_id"   { value = azuread_service_principal.sp.object_id }
