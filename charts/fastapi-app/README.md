# FastAPI App Helm Chart

Helm chart for deploying FastAPI applications on AKS, with:

- AGIC-compatible Ingress
- HPA v2 (default enabled)
- Optional VPA
- Optional zero-config health sidecar (http-echo)

## Values

Per-env values are supported using `values.<env>.yaml` and auto-detected by the deploy workflow when `namespace=<env>`.

Key values:

- image.repository, image.tag
- ingress.hosts[0].host, ingress.tls
- probes.useSidecar, healthzSidecar.enabled
- resources, hpa, vpa

See `values.yaml` for full schema. Dev defaults enabling the sidecar are in `values.dev.yaml`.
