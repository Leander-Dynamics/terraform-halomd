# storage-container module

Provisions a container within an existing Azure Storage account.

## Inputs

- `name` (`string`, required) – Name of the storage container.
- `storage_account_name` (`string`, required) – Name of the storage account that hosts the container.
- `access_type` (`string`, optional, default `private`) – Access level for the container (for example `private`, `blob`, or `container`).

