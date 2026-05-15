# Cloud Infrastructure

Terraform configurations for deploying Concoction to **AWS**, **Azure**, and **GCP**.

Each module is self-contained and deploys:
| Component | AWS | Azure | GCP |
|---|---|---|---|
| Container runtime | ECS Fargate | Container Apps | Cloud Run |
| Container registry | ECR | ACR | Artifact Registry |
| PostgreSQL | RDS | PostgreSQL Flexible Server | Cloud SQL |
| Secrets | Secrets Manager | Key Vault | Secret Manager |
| Observability | CloudWatch | Log Analytics | Cloud Logging |

Post-deployment smoke tests run automatically via a `null_resource` provisioner after each `terraform apply`.

---

## Prerequisites

- [Terraform](https://developer.hashicorp.com/terraform/downloads) ≥ 1.6
- [Docker](https://docs.docker.com/get-docker/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for smoke tests)
- Cloud CLI authenticated:
  - AWS: `aws configure` or `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` env vars
  - Azure: `az login`
  - GCP: `gcloud auth application-default login`

---

## Directory structure

```
infra/
  aws/
    main.tf                  VPC · ECR · RDS · ECS Fargate · ALB · IAM · Secrets Manager
    variables.tf
    outputs.tf
    versions.tf
    terraform.tfvars.example
  azure/
    main.tf                  Resource Group · ACR · PostgreSQL Flexible · Container Apps · Key Vault
    variables.tf
    outputs.tf
    versions.tf
    terraform.tfvars.example
  gcp/
    main.tf                  Artifact Registry · Cloud SQL · Secret Manager · Cloud Run
    variables.tf
    outputs.tf
    versions.tf
    terraform.tfvars.example
```

---

## Build and push the container image

Build the image from the repository root:

```bash
docker build -t concoction:latest .
```

### AWS — push to ECR

```bash
# Authenticate
aws ecr get-login-password --region eu-west-2 \
  | docker login --username AWS --password-stdin <account_id>.dkr.ecr.eu-west-2.amazonaws.com

# Tag and push
docker tag concoction:latest <account_id>.dkr.ecr.eu-west-2.amazonaws.com/concoction-prod:latest
docker push <account_id>.dkr.ecr.eu-west-2.amazonaws.com/concoction-prod:latest
```

Set `image_uri` in `infra/aws/terraform.tfvars` to the full URI above.

### Azure — push to ACR

```bash
# Authenticate (run terraform apply once first to create the ACR)
az acr login --name concoctionprodacr

# Tag and push
docker tag concoction:latest concoctionprodacr.azurecr.io/concoction:latest
docker push concoctionprodacr.azurecr.io/concoction:latest
```

### GCP — push to Artifact Registry

```bash
# Authenticate
gcloud auth configure-docker europe-west2-docker.pkg.dev

# Tag and push
docker tag concoction:latest europe-west2-docker.pkg.dev/<project_id>/concoction/concoction:latest
docker push europe-west2-docker.pkg.dev/<project_id>/concoction/concoction:latest
```

---

## Deploy

For each cloud, copy the example vars file and fill in values:

```bash
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars — never commit secrets
```

Then:

```bash
cd infra/aws        # (or azure / gcp)
terraform init
terraform plan
terraform apply
```

Terraform will automatically run smoke tests after a successful apply (requires .NET 10 SDK and a built smoke test project).

### Manual smoke test run

```bash
cd <repo_root>
dotnet test Concoction.Tests.Smoke \
  -e SMOKE_API_BASE_URL=<api_url> \
  -e SMOKE_API_KEY=<bootstrap_api_key>
```

If `SMOKE_API_BASE_URL` or `SMOKE_API_KEY` is not set, all tests are skipped gracefully.

---

## Outputs

After apply, Terraform prints:

| Output | Description |
|---|---|
| `api_url` | Public URL of the deployed API |
| `ecr_repository_url` / `acr_login_server` / `artifact_registry_url` | Registry push target |

---

## Teardown

```bash
terraform destroy
```

> **Note**: RDS/Cloud SQL instances have `deletion_protection = false` and `skip_final_snapshot = true` for easy teardown in non-production environments. Adjust for production use.
