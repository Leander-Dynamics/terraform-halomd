# Flask App Helm Chart

A lightweight Helm chart for deploying Flask applications on AKS, with:

- AGIC-compatible Ingress
- HPA v2 (default enabled)
- Optional VPA
- Optional zero-config health sidecar (http-echo)

## Values

Key values you may want to set per environment (via `values.<env>.yaml`):

- image.repository, image.tag
- ingress.hosts[0].host, ingress.tls
- probes.useSidecar (bool)
- healthzSidecar.enabled (bool)
- resources, hpa, vpa

See `values.yaml` for the full schema. Dev defaults enabling the sidecar are in `values.dev.yaml`.

## Zero-config health sidecar

Enable:

- `probes.useSidecar: true`
- `healthzSidecar.enabled: true`

Probes will target the sidecar’s port, which returns `200 OK` and body `ok`.

## Gunicorn runner (optional)

If your image doesn’t set an entrypoint, enable Gunicorn:

- `flask.useGunicorn: true`
- `flask.appModule: app` (e.g., `app`, `src.main`)
- `flask.workers: 2`
