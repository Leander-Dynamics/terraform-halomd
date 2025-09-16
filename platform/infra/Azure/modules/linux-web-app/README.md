# Linux Web App module

Creates an Azure Linux Web App on an existing App Service plan.

## Usage

```hcl
module "web_app" {
  source = "../../Azure/modules/linux-web-app"

  name                = "app-example"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.example.name
  service_plan_id     = azurerm_service_plan.example.id

  tags = {
    Environment = "dev"
  }
}
```

Pass the `service_plan_id` for the plan that should host the app.

`tags` is optional and lets you add Azure resource tags when needed.
