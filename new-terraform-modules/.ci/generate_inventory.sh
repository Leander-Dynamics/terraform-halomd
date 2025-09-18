#!/usr/bin/env bash
set -euo pipefail

MODULE_DIR="./modules"

echo "# Available Terraform Modules"
echo
for dir in $(find "$MODULE_DIR" -maxdepth 1 -mindepth 1 -type d); do
  name=$(basename "$dir")
  description=$(grep -m1 'description' "$dir/variables.tf" 2>/dev/null | sed -E 's/.*description *= *"(.*)".*/\1/')
  echo "- **$name**: ${description:-No description available}"
done
