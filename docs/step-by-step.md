# Step-by-step Setup (ADO)
1) Create OIDC service connections: `sc-azure-oidc-dev|qa|stage|prod`.
2) Create ADO Environments: `dev`, `qa`, `stage`, `prod` (add approvals for stage/prod).
3) Ensure `backend.tfvars` in each env points to your tfstate and `use_azuread_auth = true`.
4) Import pipeline: `azure-pipelines.yml`.
5) Open a PR to test plans; merge to apply Dev and QA; approve for stage/prod.
