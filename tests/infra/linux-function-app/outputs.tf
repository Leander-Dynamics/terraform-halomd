output "function_app_id" {
  value = module.linux_function_app.id
}

output "function_app_principal_id" {
  value = module.linux_function_app.principal_id
}

output "function_app_hostname" {
  value = module.linux_function_app.default_hostname
}

output "function_app_storage_account" {
  value = module.linux_function_app.storage_account_name
}
