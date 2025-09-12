# CI/CD Best Practices
- Use OIDC service connections (no client secrets).
- Apply the exact reviewed `.tfplan` artifact.
- Require PR + build validation to update `main`.
- Use ADO Environments for approvals on stage/prod.
