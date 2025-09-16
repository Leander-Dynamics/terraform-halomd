# Arbitration NSA Request Script — Secret Configuration

`scripts/Arbitration-SendNSARequests.ps1` retrieves a client secret at runtime so that nothing sensitive is stored in the repository. The script supports **environment variables** and **Azure Key Vault**; configure at least one option before running `SendNSARequests`.

## Option 1 — Environment variable

Set an environment variable that contains the client secret for the automation identity:

- `ARBITRATION_CLIENT_SECRET`

For multi-tenant scenarios you can provide tenant-specific overrides. Remove punctuation from the Azure AD tenant ID, convert it to uppercase, and append it to the base name with a double underscore:

- `ARBITRATION_CLIENT_SECRET__2E09F3A30520461F8474052A8ED7814A`

The script searches the Process, User, and Machine scopes and uses the first value it finds. This allows you to define the secret for all users on a jump box (Machine scope) or just the active shell session (Process scope).

## Option 2 — Azure Key Vault

If an environment variable is not set, the script attempts to resolve the secret from Azure Key Vault using the `Az.KeyVault` module. Set the following environment variables so the script knows which vault and secret to query:

- `ARBITRATION_CLIENT_SECRET_VAULT_NAME`
- `ARBITRATION_CLIENT_SECRET_SECRET_NAME`

Tenant-specific overrides follow the same naming pattern as above:

- `ARBITRATION_CLIENT_SECRET_VAULT_NAME__2E09F3A30520461F8474052A8ED7814A`
- `ARBITRATION_CLIENT_SECRET_SECRET_NAME__2E09F3A30520461F8474052A8ED7814A`

Ensure the PowerShell session is authenticated (for example with `Connect-AzAccount`) and that the identity has **Key Vault Secrets User** rights on the vault. The script raises an error if the secret cannot be retrieved from Key Vault.

## Usage reminders

- Configure either an environment variable or the Key Vault settings **per tenant** before running the script.
- Keep the secret value out of version control—store it only in secure hosts or Azure Key Vault.
- Run PowerShell with `-Verbose` to see which source (environment variable or Key Vault) supplied the client secret when troubleshooting.
