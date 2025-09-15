# mssql-server module

Provisions an Azure SQL (MSSQL) server instance.

## Inputs

- `name` (`string`, required) – Name of the SQL server resource.
- `location` (`string`, required) – Azure region where the server will be deployed.
- `resource_group_name` (`string`, required) – Resource group that hosts the server.
- `administrator_login` (`string`, required) – Login name for the SQL administrator account.
- `administrator_password` (`string`, required, sensitive) – Password for the SQL administrator account. Store this value securely (for example in Key Vault or a secret store) and pass it to Terraform as a sensitive variable.
- `sku` (`string`, optional) – SKU for the SQL server. Defaults to `Standard`.

### Example

```hcl
module "mssql_server" {
  source = "../modules/mssql-server"

  name                = "sql-prod-01"
  location            = "eastus2"
  resource_group_name = azurerm_resource_group.example.name

  administrator_login    = "sqladminuser"
  administrator_password = var.sql_admin_password
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}
```
