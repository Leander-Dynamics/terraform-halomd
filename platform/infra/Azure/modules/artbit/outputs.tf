output "resource_group_name" {
  description = "Name of the resource group that hosts the workflow resources."
  value       = module.resource_group.name
}

output "resource_group_id" {
  description = "Resource ID of the workflow resource group."
  value       = data.azurerm_resource_group.workflow.id
}

output "storage_accounts" {
  description = "Identifiers and connection details for workflow storage accounts."
  value = {
    docs = {
      name               = module.docs_storage_account.name
      id                 = module.docs_storage_account.id
      primary_access_key = module.docs_storage_account.primary_access_key
      primary_connection = module.docs_storage_account.primary_connection_string
    }
    cron = {
      name               = module.cron_storage_account.name
      id                 = module.cron_storage_account.id
      primary_access_key = module.cron_storage_account.primary_access_key
      primary_connection = module.cron_storage_account.primary_connection_string
    }
    external = {
      name               = module.external_storage_account.name
      id                 = module.external_storage_account.id
      primary_access_key = module.external_storage_account.primary_access_key
      primary_connection = module.external_storage_account.primary_connection_string
    }
  }
  sensitive = true
}

output "frontend_default_hostname" {
  description = "Default hostname assigned to the frontend web application."
  value       = azurerm_linux_web_app.frontend.default_hostname
}

output "frontend_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the frontend web application."
  value       = azurerm_private_endpoint.frontend.private_service_connection[0].private_ip_address
}

output "backend_default_hostname" {
  description = "Default hostname assigned to the backend web application."
  value       = azurerm_linux_web_app.backend.default_hostname
}

output "backend_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the backend web application."
  value       = azurerm_private_endpoint.backend.private_service_connection[0].private_ip_address
}

output "cron_function_default_hostname" {
  description = "Default hostname assigned to the cron function application."
  value       = azurerm_linux_function_app.cron.default_hostname
}

output "cron_function_private_endpoint_ip" {
  description = "Private endpoint IP allocated to the cron function application."
  value       = azurerm_private_endpoint.cron.private_service_connection[0].private_ip_address
}

output "external_function_default_hostname" {
  description = "Default hostname assigned to the external function application."
  value       = azurerm_linux_function_app.external.default_hostname
}

output "redis_cache_details" {
  description = "Redis cache identifiers and connection details (null when disabled)."
  value = var.enable_redis ? {
    id         = azurerm_redis_cache.workflow[0].id
    hostname   = azurerm_redis_cache.workflow[0].hostname
    port       = azurerm_redis_cache.workflow[0].port
    ssl_port   = azurerm_redis_cache.workflow[0].ssl_port
    access_key = azurerm_redis_cache.workflow[0].primary_access_key
  } : null
  sensitive = true
}


output "sql_server_details" {
  description = "Details for the workflow SQL server and associated databases."
  value = {
    id   = azurerm_mssql_server.workflow.id
    name = azurerm_mssql_server.workflow.name
    fqdn = azurerm_mssql_server.workflow.fully_qualified_domain_name
    databases = {
      app  = azurerm_mssql_database.app.id
      logs = azurerm_mssql_database.logs.id
    }
  }
}

output "load_balancer_details" {
  description = "Identifiers for the public load balancer."
  value = {
    id             = azurerm_lb.workflow.id
    public_ip_id   = azurerm_public_ip.workflow.id
    public_ip      = azurerm_public_ip.workflow.ip_address
    public_ip_fqdn = azurerm_public_ip.workflow.fqdn
  }
}

output "ml_virtual_machine_private_ips" {
  description = "Private IPv4 addresses allocated to ML virtual machines."
  value       = [for nic in azurerm_network_interface.ml : nic.ip_configuration[0].private_ip_address]
}

output "openai_endpoint" {
  description = "Endpoint URL for the Azure OpenAI account."
  value       = azurerm_cognitive_account.openai.endpoint
}

output "openai_primary_key" {
  description = "Primary access key for the Azure OpenAI account."
  value       = azurerm_cognitive_account.openai.primary_access_key
  sensitive   = true
}
