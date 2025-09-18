# Dev Deployment with **Branching Strategy**

We use **trunk‑based development**:

- **main** — trunk; merging triggers Dev & QA apply; stage/prod need approvals.
- **feature/*** — short‑lived branches for features.
- **hotfix/*** — urgent fixes.
- **chore/*** — docs/tooling.
- Optional: release tags.

```mermaid
gitGraph
  commit id: "init"
  branch feature/kv-endpoints
  commit id: "feat: kv endpoints (wip)"
  checkout main
  commit id: "chore: docs"
  checkout feature/kv-endpoints
  commit id: "feat: kv endpoints ready"
  checkout main
  merge feature/kv-endpoints
  branch hotfix/webapp-sku
  commit id: "fix: sku"
  checkout main
  merge hotfix/webapp-sku
```
