# AWS Infrastructure (Parity with Azure)

This folder contains AWS Terraform modules and environment stacks that mirror the Azure deployment:

- Networking: VPC, public/private subnets, NAT Gateways (hub/spoke analog)
- Platform: EKS (AKS analog) with IRSA, metrics-server, autoscaler profile
- Ingress/Edge: AWS Load Balancer Controller (ALB), optional CloudFront + AWS WAF (Front Door analog)
- Registry: ECR (ACR analog)
- Secrets: AWS Secrets Manager / SSM Parameter Store (Key Vault analog)
- Observability: CloudWatch Logs/Metrics and S3 diagnostics (Log Analytics/diag to blob analog)
- Database: RDS (Aurora PostgreSQL or SQL Server variant) (Azure SQL analog)
- DNS & Certs: Route 53 and ACM
- CI OIDC: GitHub Actions OIDC → AWS IAM roles (Managed Identity analog)

Naming follows the existing `project_name`, `env_name`, region short codes, and tags, adapted to AWS resource constraints.
