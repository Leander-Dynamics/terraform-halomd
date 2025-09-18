# State Migration
- Point new root at the same backend (init -reconfigure).
- Use `terraform state mv` for module path refactors.
- Ensure a no-op plan before switching CI/CD.
