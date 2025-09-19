# What Changed & Why

- Modularized TF under `platform/infra/Azure/modules`.
- Per-env roots with independent state backends.
- GitOps flow: PR → plan; merge → apply Dev & QA → gated stage/prod.
- Bash-only pipelines (no marketplace task dependency).

## Recent Changes

- Standardized `location` usage to `var.region` across the `artbit` module.
- Re-aligned storage account module inputs to `replication_type` and `enable_hns`; removed deprecated `sftp_enabled`.
- Removed unsupported `allow_blob_public_access` from the storage account resource (AzureRM v4 compatibility).
- Added storage account outputs: `id`, `primary_access_key`.
- Declared provider `configuration_aliases` for `azurerm.hub` to satisfy alias use.
- Added optional Redis feature flag:
	- `artbit` module now supports `variable "enable_redis"` (default `false`).
	- Each environment exposes `enable_redis` and passes it into the module.
	- When `false`, no Redis resources are created and the `redis_cache_details` output is `null`.
	- To enable in a specific environment, set `enable_redis = true` in that env’s `terraform.tfvars`.

## Rationale

These changes align the codebase with the AzureRM v4 provider schema, reduce drift across modules, and introduce an easy toggle for optional Redis usage without impacting environments where it is not needed.
