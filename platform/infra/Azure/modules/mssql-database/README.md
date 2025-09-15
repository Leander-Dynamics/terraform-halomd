# mssql-database module

Creates an Azure SQL (MSSQL) database instance.

## Inputs

- `name` (`string`, required) – Name of the database resource.
- `server_id` (`string`, required) – Resource ID of the SQL server that hosts the database.
- `sku_name` (`string`, required) – SKU to provision for the database (for example `S0`, `GP_S_Gen5_2`).

