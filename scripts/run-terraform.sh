#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Usage: $(basename "$0") <dev|qa|stage|prod>

Initializes and applies the Terraform configuration for the specified environment.
USAGE
}

if [[ $# -ne 1 ]]; then
  usage
  exit 1
fi

ENVIRONMENT="${1,,}"

case "$ENVIRONMENT" in
  dev|qa|stage|prod)
    ;;
  *)
    echo "[ERROR] Unsupported environment: $1" >&2
    usage
    exit 1
    ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_DIR="$REPO_ROOT/platform/infra/envs/$ENVIRONMENT"

if [[ ! -d "$ENV_DIR" ]]; then
  echo "[ERROR] Terraform environment directory not found: $ENV_DIR" >&2
  exit 1
fi

BACKEND_VARS_FILE="$ENV_DIR/backend.tfvars"
TFVARS_FILE="$ENV_DIR/terraform.tfvars"

if [[ ! -f "$BACKEND_VARS_FILE" ]]; then
  echo "[ERROR] backend.tfvars not found at $BACKEND_VARS_FILE" >&2
  exit 1
fi

if [[ ! -f "$TFVARS_FILE" ]]; then
  echo "[ERROR] terraform.tfvars not found at $TFVARS_FILE" >&2
  exit 1
fi

if ! command -v terraform >/dev/null 2>&1; then
  echo "[ERROR] terraform command not found in PATH." >&2
  exit 1
fi

pushd "$ENV_DIR" >/dev/null

echo "[INFO] Initializing Terraform backend for $ENVIRONMENT"
terraform init -reconfigure -backend-config="$(basename "$BACKEND_VARS_FILE")"

echo "[INFO] Applying Terraform configuration for $ENVIRONMENT"
terraform apply -var-file="$(basename "$TFVARS_FILE")" -auto-approve

popd >/dev/null
