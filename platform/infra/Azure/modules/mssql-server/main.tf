locals {
  admin_login_provided       = var.admin_login != null && trimspace(var.admin_login) != ""
  admin_password_provided    = var.admin_password != null && var.admin_password != ""
  admin_credentials_provided = local.admin_login_provided && local.admin_password_provided
  azuread_admin_provided     = var.azuread_administrator != null
}

resource "azurerm_mssql_server" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = "12.0"

  administrator_login          = local.admin_credentials_provided ? var.admin_login : null
  administrator_login_password = local.admin_credentials_provided ? var.admin_password : null

  dynamic "azuread_administrator" {
    for_each = local.azuread_admin_provided ? [var.azuread_administrator] : []

    content {
      login_username = azuread_administrator.value.login_username
      object_id      = azuread_administrator.value.object_id
      tenant_id      = try(azuread_administrator.value.tenant_id, null)
    }
  }

  lifecycle {
    precondition {
      condition     = local.admin_login_provided == local.admin_password_provided
      error_message = "Both admin_login and admin_password must be provided together."
    }

    precondition {
      condition     = local.admin_credentials_provided || local.azuread_admin_provided
      error_message = "You must provide either admin_login/admin_password or an azuread_administrator configuration."
    }
  }
}
