# What Changed & Why
- Modularized TF under `platform/infra/Azure/modules`.
- Per-env roots with independent state backends.
- GitOps flow: PR → plan; merge → apply Dev → gated stage/prod.
- Bash-only pipelines (no marketplace task dependency).
