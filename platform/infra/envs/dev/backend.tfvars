
resource_group_name = "dev-eus2-ops-rg-1"
storage_account_name = "deveus2terraform"
container_name = "arbit"
key = "arbit/dev.tfstate"
use_azuread_auth = true
# subscription_id and tenant_id are supplied at runtime (e.g. via
# AZ_SUBSCRIPTION_ID / AZ_TENANT_ID environment variables in CI or your
# local shell).
