module "linux_function_app" {
  source = "../../../platform/infra/Azure/modules/linux-function-app"

  name                = "tst-linux-func"
  resource_group_name = "rg-tst-linux-func"
  location            = "eastus2"
  plan_name           = "asp-tst-linux-func"
  plan_sku            = "Y1"
  runtime_stack       = "dotnet"
  runtime_version     = "8.0"

  application_insights_connection_string = "InstrumentationKey=00000000-0000-0000-0000-000000000000"

  app_settings = {
    "CustomSetting" = "true"
  }

  tags = {
    environment = "test"
    workload    = "linux-function-app"
  }
}
