# Python WebApp Helm Chart

Generic Helm chart for Python web apps (FastAPI/Flask/custom), with:

- Framework toggles: `framework: fastapi | flask | custom`
- Optional Gunicorn runner for Flask/custom
- AGIC-compatible Ingress
- HPA v2 and optional VPA
- Optional zero-config health sidecar

## Quickstart

- Set `image.repository` and `image.tag`.
- Set `framework` and adjust `service.targetPort` if needed (defaults per framework).
- For Flask/custom without an entrypoint, enable the `runner.useGunicorn` and set `runner.appModule`.
- Add per-env overrides as `values.<env>.yaml`.

## Zero-config health sidecar

Enable:

- `probes.useSidecar: true`
- `healthzSidecar.enabled: true`

Probes will target the sidecar’s port, which returns `200 OK` and body `ok`.
