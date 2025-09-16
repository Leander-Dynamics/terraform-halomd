#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: deploy-arbitration.sh -g <resource-group> -n <app-name> -e <environment> [options]

Required arguments:
  -g, --resource-group   Azure resource group that hosts the App Service
  -n, --app-name         App Service name
  -e, --environment      Environment name (dev, stage, prod, etc.)

Optional arguments:
  -c, --configuration    dotnet publish configuration (default: Release)
  -o, --output           Output directory for build artifacts (default: ./artifacts/arbitration/<environment>)
  -s, --slot             Deploy to a specific App Service slot
      --use-zip-deploy   Use legacy az webapp deployment source config-zip instead of az webapp deploy
  -h, --help             Show this help message
USAGE
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Error: Required command '$1' is not available on PATH." >&2
    exit 1
  fi
}

trim() {
  local var="$*"
  var="${var#${var%%[![:space:]]*}}"
  var="${var%${var##*[![:space:]]}}"
  printf '%s' "$var"
}

require_command dotnet
require_command az
require_command npm
require_command zip

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

RESOURCE_GROUP=""
APP_NAME=""
ENVIRONMENT=""
CONFIGURATION="Release"
OUTPUT_DIR=""
SLOT=""
DEPLOY_METHOD="deploy"

while [[ $# -gt 0 ]]; do
  case "$1" in
    -g|--resource-group)
      RESOURCE_GROUP="$2"
      shift 2
      ;;
    -n|--app-name)
      APP_NAME="$2"
      shift 2
      ;;
    -e|--environment)
      ENVIRONMENT="$2"
      shift 2
      ;;
    -c|--configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    -o|--output)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    -s|--slot)
      SLOT="$2"
      shift 2
      ;;
    --use-zip-deploy)
      DEPLOY_METHOD="zip-deploy"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
  esac
done

RESOURCE_GROUP="$(trim "$RESOURCE_GROUP")"
APP_NAME="$(trim "$APP_NAME")"
ENVIRONMENT="$(trim "$ENVIRONMENT")"
CONFIGURATION="$(trim "$CONFIGURATION")"
SLOT="$(trim "$SLOT")"
OUTPUT_DIR="$(trim "$OUTPUT_DIR")"

if [[ -z "$RESOURCE_GROUP" || -z "$APP_NAME" || -z "$ENVIRONMENT" ]]; then
  echo "Error: resource group, app name, and environment are required." >&2
  usage
  exit 1
fi

if [[ -z "$CONFIGURATION" ]]; then
  CONFIGURATION="Release"
fi

SOLUTION_PATH="$REPO_ROOT/Arbitration/MPArbitration.sln"
CLIENT_APP_PATH="$REPO_ROOT/Arbitration/MPArbitration/ClientApp"

if [[ ! -f "$SOLUTION_PATH" ]]; then
  echo "Error: Solution not found at $SOLUTION_PATH" >&2
  exit 1
fi

if [[ ! -d "$CLIENT_APP_PATH" ]]; then
  echo "Error: Client app folder not found at $CLIENT_APP_PATH" >&2
  exit 1
fi

if [[ -n "$OUTPUT_DIR" ]]; then
  mkdir -p "$OUTPUT_DIR"
  ARTIFACT_ROOT="$(cd "$OUTPUT_DIR" && pwd)"
else
  ARTIFACT_ROOT="$REPO_ROOT/artifacts/arbitration/$ENVIRONMENT"
  mkdir -p "$ARTIFACT_ROOT"
fi

PUBLISH_DIR="$ARTIFACT_ROOT/publish"
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"

echo "Restoring .NET solution..."
dotnet restore "$SOLUTION_PATH" --nologo

echo "Installing client dependencies..."
npm ci --prefix "$CLIENT_APP_PATH"

CLIENT_BUILD_SCRIPT="build"
case "${ENVIRONMENT,,}" in
  dev|development)
    CLIENT_BUILD_SCRIPT="build-dev"
    ;;
  stage|staging)
    CLIENT_BUILD_SCRIPT="build-stage"
    ;;
  prod|production)
    CLIENT_BUILD_SCRIPT="build"
    ;;
  *)
    CLIENT_BUILD_SCRIPT="build"
    ;;
 esac

echo "Building client app with npm run $CLIENT_BUILD_SCRIPT..."
npm run "$CLIENT_BUILD_SCRIPT" --prefix "$CLIENT_APP_PATH"

echo "Publishing .NET solution to $PUBLISH_DIR..."
dotnet publish "$SOLUTION_PATH" --configuration "$CONFIGURATION" --no-restore --output "$PUBLISH_DIR" --nologo

TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
ZIP_PATH="$ARTIFACT_ROOT/MPArbitration-$ENVIRONMENT-$TIMESTAMP.zip"
rm -f "$ZIP_PATH"

echo "Packaging publish folder into $ZIP_PATH..."
(
  cd "$PUBLISH_DIR"
  zip -qr "$ZIP_PATH" .
)

if [[ "$DEPLOY_METHOD" == "deploy" ]]; then
  AZ_CMD=(az webapp deploy --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --src-path "$ZIP_PATH" --type zip)
else
  AZ_CMD=(az webapp deployment source config-zip --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --src "$ZIP_PATH")
fi

if [[ -n "$SLOT" ]]; then
  AZ_CMD+=(--slot "$SLOT")
fi

echo "Deploying package to Azure Web App '$APP_NAME' (resource group '$RESOURCE_GROUP')..."
"${AZ_CMD[@]}"

echo "Deployment complete. Package path: $ZIP_PATH"
