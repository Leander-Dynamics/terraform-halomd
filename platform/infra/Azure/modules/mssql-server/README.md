# mssql-server module

Provision an Azure SQL Server instance that supports either SQL authentication or Azure AD administration.

## Inputs

- `name` (`string`, required) – Name of the SQL server resource.
- `location` (`string`, required) – Azure region where the server is deployed.
- `resource_group_name` (`string`, required) – Resource group that will contain the server.
- `admin_login` (`string`, optional) – SQL administrator login to provision. Must be provided together with `admin_password` when using SQL authentication.
- `admin_password` (`string`, optional) – SQL administrator password to provision. Must be provided together with `admin_login` when using SQL authentication.
- `azuread_administrator` (`object`, optional) – Azure AD administrator settings for the server. When supplied, it must include `login_username` and `object_id`, and it can optionally include `tenant_id`.

Either the SQL administrator credentials (`admin_login` and `admin_password`) or an `azuread_administrator` block must be provided to satisfy Azure API requirements.
