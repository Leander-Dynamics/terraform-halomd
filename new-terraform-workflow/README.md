

## Terraform Workflow
This projects aims to isolate the workflow project into its own repo and state file and hopefully serve as a skeleton for other projects to come.



### Setup
You will have to have these following environment specific files ready, this will allow us to abstract the code and make it applicable for all environments but customizable using these files. 
```
dev.tfvars
qa.tfvars
prod.tfvars
```
The contents of dev.tfvars looks like this as example:
```
environment="dev"
environment_label="Development"
env_region="dev-eus2"
region="eastus2"
region_short="eus2"
workflow_storage_account="deveus2workflowsa"
```

Later on when we are ready to set up a production environment, we will simply have something like this called prod.tfvars
```
environment="prod"
environment_label="Production"
env_region="prod-eus2"
region="eastus2"
region_short="eus2"
workflow_storage_account="prodeus2workflowsa"
```

### Authenticate 

```
az login
```

## For Development Environment:
```
az account set --subscription="930755b1-ef22-4721-a31a-1b6fbecf7da6" # explicitly set the subscription to this for development

# to ensure one is on development subscription # HaloMD Development
az account show
az account show --query id -o tsv # look at subscription id alone 

# initialize backend for a specific environment. so for development environment
terraform init -backend-config=backend-dev.tfvars # can run this with -reconfigure if the backend needs to be reconfigured

# for qa & prod it would be in backend-qa.tfvars and backend-prod.tfvars 

# plan
terraform plan -out=development.tfplan -var-file=dev.tfvars

# apply
terraform apply "development.tfplan" # apply this specific plan
```

## For QA Environment:
```
az account set --subscription="205da762-4b21-4105-94de-edf6799de330" # explicitly set the subscription to this for qa

# to ensure one is on qa subscription # HaloMD QA
az account show
az account show --query id -o tsv # look at subscription id alone 

# initialize backend for a specific environment. So for QA environment
terraform init -backend-config=backend-qa.tfvars

# if you need to reconfigure the backend
terraform init -backend-config=backend-qa.tfvars -reconfigure

# plan
terraform plan -out=qa.tfplan -var-file=qa.tfvars

# apply
terraform apply "qa.tfplan" # apply this specific plan
```

## For PROD Environment:
```
az account set --subscription="40f3e169-b544-4789-936a-5526146e3b8e" # explicitly set the subscription to this for prod

# to ensure one is on production subscription # HaloMD Production
az account show
az account show --query id -o tsv # look at subscription id alone 

# initialize backend for a specific environment. So for Production environment
terraform init -backend-config=backend-prod.tfvars

# plan
terraform plan -out=prod.tfplan -var-file=prod.tfvars

# apply
terraform apply "prod.tfplan" # apply this specific plan
```