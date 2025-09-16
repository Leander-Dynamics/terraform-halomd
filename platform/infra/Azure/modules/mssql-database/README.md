# mssql-database module

Creates an Azure SQL (MSSQL) database instance.

## Inputs

- `name` (`string`, required) – Name of the database resource.
- `server_id` (`string`, required) – Resource ID of the SQL server that hosts the database.
- `sku_name` (`string`, required) – SKU to provision for the database (for example `S0`, `GP_S_Gen5_2`).
- `tags` (`map(string)`, optional) – Tags to apply to the SQL database resource.

## Example

```hcl
module "mssql_database" {
  source = "../modules/mssql-database"

  name      = "sql-db-01"
  server_id = azurerm_mssql_server.example.id
  sku_name  = "S0"

  tags = {
    Environment = "dev"
  }
}
```

