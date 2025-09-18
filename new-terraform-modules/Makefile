# ====== Tools / Versions ======
TERRAFORM ?= terraform
TFLINT ?= tflint
TFDOCS ?= terraform-docs
CHGLOG ?= git-chglog

# ====== Phony Targets ======
.PHONY: init fmt lint validate test inventory docs changelog ci

# ====== Terraform Commands ======
init: ## Initialize Terraform
	$(TERRAFORM) init

fmt: ## Format Terraform code
	$(TERRAFORM) fmt -recursive

lint: ## Lint Terraform code
	$(TFLINT) --recursive

validate: ## Validate each Terraform module
	@for d in modules/*; do \
		if [ -f "$$d/main.tf" ]; then \
			echo "Validating $$d"; \
			(cd $$d && $(TERRAFORM) init -backend=false -input=false >/dev/null && $(TERRAFORM) validate); \
		fi; \
	done

test: ## Test each Terraform module (using validate for now)
	@for d in modules/*; do \
		if [ -f "$$d/main.tf" ]; then \
			echo "Testing $$d"; \
			(cd $$d && $(TERRAFORM) init -backend=false -input=false >/dev/null && $(TERRAFORM) validate); \
		fi; \
	done
# Note: Replace with terratest or other automated tests when ready

# ====== Documentation ======
inventory: ## Generate MODULES.md inventory
	@./.ci/generate_inventory.sh > MODULES.md

docs: ## Generate terraform-docs for each module
	@for dir in modules/*; do \
		if [ -d "$$dir" ] && [ -f "$$dir/main.tf" ]; then \
			echo "Generating docs for $$dir"; \
			$(TFDOCS) markdown table $$dir > $$dir/README.auto.md; \
		fi; \
	done

changelog: ## Generate changelog entry from commits since last tag
	$(CHGLOG) --next-tag $$(git describe --tags --abbrev=0 2>/dev/null || echo v0.1.0) > CHANGELOG.md

# ====== CI ======
ci: fmt lint validate test inventory docs ## Run all checks for CI
	@echo "All CI checks completed successfully."
