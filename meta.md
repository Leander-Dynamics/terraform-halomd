### Resources Found (Grouped by Type)

- [x] _lb
- [x] _lb_rule
- [x] _linux_function_app
- [x] _linux_virtual_machine
- [x] _linux_web_app
- [x] _managed_disk
- [x] _mssql_database
- [x] _mssql_server
- [x] _network_security_group
- [x] _storage_container
- [x] _virtual_machine_data_disk_attachment

#### azurerm_service_plan (19)
| Resource Name                | File                                                    |
|------------------------------|---------------------------------------------------------|
| arbitv2poc_service_plan      | terraform/development/arbitv2poc.tf                     |
| idrereselection_service_plan | terraform/development/idrereselection.tf                |
| idre_service_plan            | terraform/development/idre_api.tf                       |
| appserviceplan               | terraform/development/invoicing_platform_app_service.tf |
| lokilogging_service_plan     | terraform/development/lokilogging.tf                    |
| sendgrid_events_service_plan | terraform/development/sendgrid_apis.tf                  |
| workflow_be                  | terraform/development/workflow.tf                       |
| workflow_func                | terraform/development/workflow.tf                       |
| arbitv2poc_service_plan      | terraform/production/arbitv2poc.tf                      |
| idre_service_plan            | terraform/production/idre.tf                            |
| idrereselection_service_plan | terraform/production/idrereselection.tf                 |
| api-appserviceplan           | terraform/production/invoicing_platform_app_service.tf  |
| web-appserviceplan           | terraform/production/invoicing_platform_app_service.tf  |
| lokilogging_service_plan     | terraform/production/lokilogging.tf                     |
| sendgrid_events_service_plan | terraform/production/sendgrid_apis.tf                   |
| api-appserviceplan           | terraform/qa/invoicing_platform_app_service.tf          |
| web-appserviceplan           | terraform/qa/invoicing_platform_app_service.tf          |
| lokilogging_service_plan     | terraform/qa/lokilogging.tf                             |
| lokilogging_service_plan     | terraform/staging/lokilogging.tf                        |

#### azurerm_linux_function_app (17)
| Resource Name                     | File                                          |
|-----------------------------------|-----------------------------------------------|
| arbitv2poc_function_app           | terraform/development/arbitv2poc.tf           |
| email_deliverability_function_app | terraform/development/email_deliverability.tf |
| idrereselection_function_app      | terraform/development/idrereselection.tf      |
| idre_function_app                 | terraform/development/idre_api.tf             |
| lokilogging_function_app          | terraform/development/lokilogging.tf          |
| sendgrid_webhook_function_app     | terraform/development/sendgrid_apis.tf        |
| sendgrid_events_function_app      | terraform/development/sendgrid_apis.tf        |
| workflow-cron-task                | terraform/development/workflow.tf             |
| arbitv2poc_function_app           | terraform/production/arbitv2poc.tf            |
| email_deliverability_function_app | terraform/production/email_deliverability.tf  |
| idre_function_app                 | terraform/production/idre.tf                  |
| idrereselection_function_app      | terraform/production/idrereselection.tf       |
| lokilogging_function_app          | terraform/production/lokilogging.tf           |
| sendgrid_webhook_function_app     | terraform/production/sendgrid_apis.tf         |
| sendgrid_events_function_app      | terraform/production/sendgrid_apis.tf         |
| lokilogging_function_app          | terraform/qa/lokilogging.tf                   |
| lokilogging_function_app          | terraform/staging/lokilogging.tf              |

#### azurerm_app_service_virtual_network_swift_connection (16)
| Resource Name                         | File                                          |
|---------------------------------------|-----------------------------------------------|
| arbitv2poc_vnet_integration           | terraform/development/arbitv2poc.tf           |
| email_deliverability_vnet_integration | terraform/development/email_deliverability.tf |
| idrereselection_vnet_integration      | terraform/development/idrereselection.tf      |
| idre_vnet_integration                 | terraform/development/idre_api.tf             |
| vnet_integration                      | terraform/development/lokilogging.tf          |
| sengrid_webhook_vnet_integration      | terraform/development/sendgrid_apis.tf        |
| sendgrid_events_vnet_integration      | terraform/development/sendgrid_apis.tf        |
| arbitv2poc_vnet_integration           | terraform/production/arbitv2poc.tf            |
| email_deliverability_vnet_integration | terraform/production/email_deliverability.tf  |
| idre_vnet_integration                 | terraform/production/idre.tf                  |
| idrereselection_vnet_integration      | terraform/production/idrereselection.tf       |
| vnet_integration                      | terraform/production/lokilogging.tf           |
| sengrid_webhook_vnet_integration      | terraform/production/sendgrid_apis.tf         |
| sendgrid_events_vnet_integration      | terraform/production/sendgrid_apis.tf         |
| vnet_integration                      | terraform/qa/lokilogging.tf                   |
| vnet_integration                      | terraform/staging/lokilogging.tf              |

#### azurerm_private_endpoint (12)
| Resource Name                     | File                                          |
|-----------------------------------|-----------------------------------------------|
| arbitv2poc_func_private           | terraform/development/arbitv2poc.tf           |
| email_deliverability_func_private | terraform/development/email_deliverability.tf |
| storage_pe                        | terraform/development/fileserver.tf           |
| idre_func_private                 | terraform/development/idre_api.tf             |
| sendgrid_events_func_private_ep   | terraform/development/sendgrid_apis.tf        |
| workflow_redis_pe                 | terraform/development/workflow.tf             |
| sql_pe                            | terraform/development/workflow.tf             |
| res-35                            | terraform/hub/main.aztfexport.tf              |
| arbitv2poc_func_private           | terraform/production/arbitv2poc.tf            |
| email_deliverability_func_private | terraform/production/email_deliverability.tf  |
| idre_func_private                 | terraform/production/idre.tf                  |
| sendgrid_events_func_private_ep   | terraform/production/sendgrid_apis.tf         |

#### azurerm_private_dns_a_record (28)
| Resource Name                        | File                                          |
|--------------------------------------|-----------------------------------------------|
| arbitv2poc_function_record           | terraform/development/arbitv2poc.tf           |
| appplatform_db_record                | terraform/development/databases.tf            |
| email_deliverability_function_record | terraform/development/email_deliverability.tf |
| function_record                      | terraform/development/idre_api.tf             |
| sendgrid_events_function_a_record    | terraform/development/sendgrid_apis.tf        |
| temporal_dns_ecord                   | terraform/development/temporal.tf             |
| temporal_dns_record_global           | terraform/development/temporal.tf             |
| dataproc_dns_record                  | terraform/development/vm_dataproc.tf          |
| uipath_dns_record                    | terraform/development/vm_uipath.tf            |
| res-25                               | terraform/hub/main.aztfexport.tf              |
| res-26                               | terraform/hub/main.aztfexport.tf              |
| res-27                               | terraform/hub/main.aztfexport.tf              |
| res-28                               | terraform/hub/main.aztfexport.tf              |
| res-29                               | terraform/hub/main.aztfexport.tf              |
| res-30                               | terraform/hub/main.aztfexport.tf              |
| res-31                               | terraform/hub/main.aztfexport.tf              |
| res-32                               | terraform/hub/main.aztfexport.tf              |
| arbitv2poc_function_record           | terraform/production/arbitv2poc.tf            |
| appplatform_db_record                | terraform/production/databases.tf             |
| octopus-production-eus2              | terraform/production/dns.tf                   |
| octopus_production                   | terraform/production/dns.tf                   |
| email_deliverability_function_record | terraform/production/email_deliverability.tf  |
| function_record                      | terraform/production/idre.tf                  |
| sendgrid_events_function_a_record    | terraform/production/sendgrid_apis.tf         |
| temporal_dns_record                  | terraform/production/temporal.tf              |
| temporal_dns_record_global           | terraform/production/temporal.tf              |
| appplatform_db_record                | terraform/qa/databases.tf                     |
| uipath_dns_record                    | terraform/qa/vm_uipath.tf                     |

#### azurerm_storage_account (27)
| Resource Name           | File                                       |
|-------------------------|--------------------------------------------|
| terraform               | terraform/development/backend.tf           |
| dwsql_sa                | terraform/development/dwsql.tf             |
| fileserver_sa           | terraform/development/fileserver.tf        |
| idrereselection_storage | terraform/development/idrereselection.tf   |
| lokilogging_storage     | terraform/development/lokilogging.tf       |
| rssa                    | terraform/development/recovery_services.tf |
| dataplatform            | terraform/development/snowflake.tf         |
| apps                    | terraform/development/storage.tf           |
| workflow_storage        | terraform/development/workflow.tf          |
| terraform               | terraform/hub/backend.tf                   |
| terraform               | terraform/production/backend.tf            |
| dwsql_sa                | terraform/production/dwsql.tf              |
| idrereselection_storage | terraform/production/idrereselection.tf    |
| lokilogging_storage     | terraform/production/lokilogging.tf        |
| rssa                    | terraform/production/recovery_services.tf  |
| dataplatform            | terraform/production/snowflake.tf          |
| databasebackups         | terraform/production/snowflake.tf          |
| apps                    | terraform/production/storage.tf            |
| nsalegalaffairs         | terraform/production/storage.tf            |
| terraform               | terraform/qa/backend.tf                    |
| dwsql_sa                | terraform/qa/dwsql.tf                      |
| lokilogging_storage     | terraform/qa/lokilogging.tf                |
| apps                    | terraform/qa/storage.tf                    |
| terraform               | terraform/staging/backend.tf               |
| lokilogging_storage     | terraform/staging/lokilogging.tf           |
| dataplatform            | terraform/staging/snowflake.tf             |
| apps                    | terraform/staging/storage.tf               |

#### azurerm_storage_container (22)
| Resource Name           | File                                   |
|-------------------------|----------------------------------------|
| common                  | terraform/development/backend.tf       |
| dwsql_container         | terraform/development/dwsql.tf         |
| idre_api_container      | terraform/development/idre_api.tf      |
| dataplatform            | terraform/development/snowflake.tf     |
| idre                    | terraform/development/storage.tf       |
| loki                    | terraform/development/vm_monitoring.tf |
| workflow_docs_container | terraform/development/workflow.tf      |
| tfstate                 | terraform/hub/backend.tf               |
| tfstate                 | terraform/production/backend.tf        |
| dwsql_container         | terraform/production/dwsql.tf          |
| dataplatform            | terraform/production/snowflake.tf      |
| databasebackups         | terraform/production/snowflake.tf      |
| idre                    | terraform/production/storage.tf        |
| nsalegalaffairs         | terraform/production/storage.tf        |
| loki                    | terraform/production/vm_monitoring.tf  |
| tfstate                 | terraform/qa/backend.tf                |
| dwsql_container         | terraform/qa/dwsql.tf                  |
| apps                    | terraform/qa/storage.tf                |
| loki                    | terraform/qa/vm_monitoring.tf          |
| tfstate                 | terraform/staging/backend.tf           |
| dataplatform            | terraform/staging/snowflake.tf         |
| idre                    | terraform/staging/storage.tf           |

#### azurerm_private_dns_zone (16)
| Resource Name         | File                               |
|-----------------------|------------------------------------|
| postgres-zone         | terraform/development/databases.tf |
| snowflake-privatelink | terraform/development/snowflake.tf |
| redis_dns             | terraform/development/workflow.tf  |
| workflow_sql_dns      | terraform/development/workflow.tf  |
| res-24                | terraform/hub/main.aztfexport.tf   |
| postgres-zone         | terraform/production/databases.tf  |
| production            | terraform/production/dns.tf        |
| production-eus2       | terraform/production/dns.tf        |
| snowflake-privatelink | terraform/production/snowflake.tf  |
| postgres-zone         | terraform/qa/databases.tf          |
| qa                    | terraform/qa/dns.tf                |
| qa-eus2               | terraform/qa/dns.tf                |
| postgres-zone         | terraform/staging/databases.tf     |
| staging               | terraform/staging/dns.tf           |
| staging-eus2          | terraform/staging/dns.tf           |
| snowflake-privatelink | terraform/staging/snowflake.tf     |

#### azurerm_private_dns_zone_virtual_network_link (13)
| Resource Name         | File                               |
|-----------------------|------------------------------------|
| postgres-vlink        | terraform/development/databases.tf |
| redis_dns_link        | terraform/development/workflow.tf  |
| sql_dns_link          | terraform/development/workflow.tf  |
| res-34                | terraform/hub/main.aztfexport.tf   |
| postgres-vlink        | terraform/production/databases.tf  |
| production-vlink      | terraform/production/dns.tf        |
| production-eus2-vlink | terraform/production/dns.tf        |
| postgres-vlink        | terraform/qa/databases.tf          |
| qa-vlink              | terraform/qa/dns.tf                |
| qa-eus2-vlink         | terraform/qa/dns.tf                |
| postgres-vlink        | terraform/staging/databases.tf     |
| staging-vlink         | terraform/staging/dns.tf           |
| staging-eus2-vlink    | terraform/staging/dns.tf           |

#### azurerm_postgresql_flexible_server (7)
| Resource Name          | File                               |
|------------------------|------------------------------------|
| dataplatform-db-server | terraform/development/databases.tf |
| appplatform-db-server  | terraform/development/databases.tf |
| dataplatform-db-server | terraform/production/databases.tf  |
| appplatform-db-server  | terraform/production/databases.tf  |
| dataplatform-db-server | terraform/qa/databases.tf          |
| appplatform-db-server  | terraform/qa/databases.tf          |
| dataplatform-db-server | terraform/staging/databases.tf     |

#### azurerm_postgresql_flexible_server_database (8)
| Resource Name          | File                               |
|------------------------|------------------------------------|
| benchmarkdb            | terraform/development/databases.tf |
| halomddb               | terraform/development/databases.tf |
| temporaldb             | terraform/development/temporal.tf  |
| temporal_visibility_db | terraform/development/temporal.tf  |
| halomddb               | terraform/production/databases.tf  |
| temporaldb             | terraform/production/temporal.tf   |
| temporal_visibility_db | terraform/production/temporal.tf   |
| halomddb               | terraform/qa/databases.tf          |

#### azurerm_network_security_group (42)
| Resource Name       | File                                        |
|---------------------|---------------------------------------------|
| dwsql_nsg           | terraform/development/dwsql.tf              |
| temporal-nsg        | terraform/development/temporal.tf           |
| airbyte-nsg         | terraform/development/vm_airbyte.tf         |
| briefbuilder-nsg    | terraform/development/vm_briefbuilder.tf    |
| dagster-nsg         | terraform/development/vm_dagster.tf         |
| dataproc-nsg        | terraform/development/vm_dataproc.tf        |
| monitoring-nsg      | terraform/development/vm_monitoring.tf      |
| openmetadata-nsg    | terraform/development/vm_openmetadata.tf    |
| taskautomations-nsg | terraform/development/vm_taskautomations.tf |
| uipath-nsg          | terraform/development/vm_uipath.tf          |
| webapps-nsg         | terraform/development/vm_webapps.tf         |
| workflow-nsg        | terraform/development/workflow.tf           |
| builder-nsg         | terraform/development/deprecated/builder.tf |
| octopus-nsg         | terraform/development/deprecated/octopus.tf |
| wireguard-nsg       | terraform/development/unused/wireguard.tf   |
| builder-nsg         | terraform/production/builder.tf             |
| dwsql_nsg           | terraform/production/dwsql.tf               |
| octopus-nsg         | terraform/production/octopus.tf             |
| temporal-nsg        | terraform/production/temporal.tf            |
| airbyte-nsg         | terraform/production/vm_airbyte.tf          |
| briefbuilder-nsg    | terraform/production/vm_briefbuilder.tf     |
| dagster-nsg         | terraform/production/vm_dagster.tf          |
| devops-nsg          | terraform/production/vm_devops.tf           |
| monitoring-nsg      | terraform/production/vm_monitoring.tf       |
| openmetadata-nsg    | terraform/production/vm_openmetadata.tf     |
| taskautomations-nsg | terraform/production/vm_taskautomations.tf  |
| webapps-nsg         | terraform/production/vm_webapps.tf          |
| arbitdemo-nsg       | terraform/qa/arbitdemo.tf                   |
| dwsql_nsg           | terraform/qa/dwsql.tf                       |
| briefbuilder-nsg    | terraform/qa/vm_briefbuilder.tf             |
| dagster-nsg         | terraform/qa/vm_dagster.tf                  |
| monitoring-nsg      | terraform/qa/vm_monitoring.tf               |
| taskautomations-nsg | terraform/qa/vm_taskautomations.tf          |
| uipath-nsg          | terraform/qa/vm_uipath.tf                   |
| webapps-nsg         | terraform/qa/vm_webapps.tf                  |
| airbyte-nsg         | terraform/qa/stage/vm_airbyte.tf            |
| airbyte-nsg         | terraform/staging/vm_airbyte.tf             |
| briefbuilder-nsg    | terraform/staging/vm_briefbuilder.tf        |
| dagster-nsg         | terraform/staging/vm_dagster.tf             |
| openmetadata-nsg    | terraform/staging/vm_openmetadata.tf        |
| taskautomations-nsg | terraform/staging/vm_taskautomations.tf     |
| webapps-nsg         | terraform/staging/vm_webapps.tf             |

#### azurerm_subnet_network_security_group_association (3)
| Resource Name   | File                           |
|-----------------|--------------------------------|
| mssql_nsg_assoc | terraform/development/dwsql.tf |
| mssql_nsg_assoc | terraform/production/dwsql.tf  |
| mssql_nsg_assoc | terraform/qa/dwsql.tf          |

#### azurerm_route_table (3)
| Resource Name | File                           |
|---------------|--------------------------------|
| mssql_rt      | terraform/development/dwsql.tf |
| mssql_rt      | terraform/production/dwsql.tf  |
| mssql_rt      | terraform/qa/dwsql.tf          |

#### azurerm_subnet_route_table_association (3)
| Resource Name  | File                           |
|----------------|--------------------------------|
| mssql_rt_assoc | terraform/development/dwsql.tf |
| mssql_rt_assoc | terraform/production/dwsql.tf  |
| mssql_rt_assoc | terraform/qa/dwsql.tf          |

#### azurerm_mssql_managed_instance (3)
| Resource Name | File                           |
|---------------|--------------------------------|
| dwsql_db      | terraform/development/dwsql.tf |
| dwsql_db      | terraform/production/dwsql.tf  |
| dwsql_db      | terraform/qa/dwsql.tf          |

#### azurerm_mssql_managed_database (3)
| Resource Name   | File                           |
|-----------------|--------------------------------|
| dwsql_databases | terraform/development/dwsql.tf |
| dwsql_databases | terraform/production/dwsql.tf  |
| dwsql_databases | terraform/qa/dwsql.tf          |

#### azurerm_private_dns_cname_record (3)
| Resource Name | File                           |
|---------------|--------------------------------|
| db_alias      | terraform/development/dwsql.tf |
| db_alias      | terraform/production/dwsql.tf  |
| db_alias      | terraform/qa/dwsql.tf          |

#### azurerm_role_assignment (14)
| Resource Name                        | File                                          |
|--------------------------------------|-----------------------------------------------|
| email_deliverability_kv_secrets_user | terraform/development/email_deliverability.tf |
| fileshare_brief_group                | terraform/development/fileserver.tf           |
| idre_func_blob_reader                | terraform/development/idre_api.tf             |
| sendgrid_webhook_kv_secrets_user     | terraform/development/sendgrid_apis.tf        |
| sendgrid_events_kv_secrets_user      | terraform/development/sendgrid_apis.tf        |
| vm_admin_login                       | terraform/development/vm_dataproc.tf          |
| uipathvm_user_login                  | terraform/development/vm_uipath.tf            |
| uipathvm_admin_login                 | terraform/development/vm_uipath.tf            |
| workflow_blob_access                 | terraform/development/workflow.tf             |
| email_deliverability_kv_secrets_user | terraform/production/email_deliverability.tf  |
| sendgrid_webhook_kv_secrets_user     | terraform/production/sendgrid_apis.tf         |
| sendgrid_events_kv_secrets_user      | terraform/production/sendgrid_apis.tf         |
| vm_user_login                        | terraform/qa/vm_uipath.tf                     |
| vm_admin_login                       | terraform/qa/vm_uipath.tf                     |

#### azurerm_application_insights (2)
| Resource Name                     | File                                          |
|-----------------------------------|-----------------------------------------------|
| email_deliverability_app_insights | terraform/development/email_deliverability.tf |
| email_deliverability_app_insights | terraform/production/email_deliverability.tf  |

#### azurerm_storage_share (1)
| Resource Name    | File                                |
|------------------|-------------------------------------|
| fileserver_share | terraform/development/fileserver.tf |

#### azurerm_container_registry (8)
| Resource Name | File                                    |
|---------------|-----------------------------------------|
| apps          | terraform/development/infrastructure.tf |
| data          | terraform/development/infrastructure.tf |
| apps          | terraform/production/infrastructure.tf  |
| data          | terraform/production/infrastructure.tf  |
| apps          | terraform/qa/infrastructure.tf          |
| data          | terraform/qa/infrastructure.tf          |
| apps          | terraform/staging/infrastructure.tf     |
| data          | terraform/staging/infrastructure.tf     |

#### azurerm_key_vault (8)
| Resource Name | File                                    |
|---------------|-----------------------------------------|
| apps          | terraform/development/infrastructure.tf |
| data          | terraform/development/infrastructure.tf |
| apps          | terraform/production/infrastructure.tf  |
| data          | terraform/production/infrastructure.tf  |
| apps          | terraform/qa/infrastructure.tf          |
| data          | terraform/qa/infrastructure.tf          |
| apps          | terraform/staging/infrastructure.tf     |
| data          | terraform/staging/infrastructure.tf     |

#### azurerm_public_ip (11)
| Resource Name         | File                                      |
|-----------------------|-------------------------------------------|
| nat_gateway_ip        | terraform/development/infrastructure.tf   |
| workflow_lb_public_ip | terraform/development/workflow.tf         |
| bastion               | terraform/development/unused/bastion.tf   |
| wireguard-pip         | terraform/development/unused/wireguard.tf |
| wireguard-ipv6-pip    | terraform/development/unused/wireguard.tf |
| res-36                | terraform/hub/main.aztfexport.tf          |
| res-37                | terraform/hub/main.aztfexport.tf          |
| res-38                | terraform/hub/main.aztfexport.tf          |
| nat_gateway_ip        | terraform/production/infrastructure.tf    |
| nat_gateway_ip        | terraform/qa/infrastructure.tf            |
| nat_gateway_ip        | terraform/staging/infrastructure.tf       |

#### azurerm_nat_gateway (5)
| Resource Name | File                                    |
|---------------|-----------------------------------------|
| nat_gateway   | terraform/development/infrastructure.tf |
| res-22        | terraform/hub/main.aztfexport.tf        |
| nat_gateway   | terraform/production/infrastructure.tf  |
| nat_gateway   | terraform/qa/infrastructure.tf          |
| nat_gateway   | terraform/staging/infrastructure.tf     |

#### azurerm_nat_gateway_public_ip_association (5)
| Resource Name              | File                                    |
|----------------------------|-----------------------------------------|
| nat_gateway_ip_association | terraform/development/infrastructure.tf |
| res-23                     | terraform/hub/main.aztfexport.tf        |
| nat_gateway_ip_association | terraform/production/infrastructure.tf  |
| nat_gateway_ip_association | terraform/qa/infrastructure.tf          |
| nat_gateway_ip_association | terraform/staging/infrastructure.tf     |

#### azurerm_subnet_nat_gateway_association (5)
| Resource Name      | File                                    |
|--------------------|-----------------------------------------|
| subnet_nat_gateway | terraform/development/infrastructure.tf |
| res-46             | terraform/hub/main.aztfexport.tf        |
| subnet_nat_gateway | terraform/production/infrastructure.tf  |
| subnet_nat_gateway | terraform/qa/infrastructure.tf          |
| subnet_nat_gateway | terraform/staging/infrastructure.tf     |

#### azurerm_linux_web_app (7)
| Resource Name   | File                                                    |
|-----------------|---------------------------------------------------------|
| invoice-api     | terraform/development/invoicing_platform_app_service.tf |
| invoice-web     | terraform/development/invoicing_platform_app_service.tf |
| workflow_webapp | terraform/development/workflow.tf                       |
| invoice-api     | terraform/production/invoicing_platform_app_service.tf  |
| invoice-web     | terraform/production/invoicing_platform_app_service.tf  |
| invoice-api     | terraform/qa/invoicing_platform_app_service.tf          |
| invoice-web     | terraform/qa/invoicing_platform_app_service.tf          |

#### azurerm_virtual_network (5)
| Resource Name | File                             |
|---------------|----------------------------------|
| vnet          | terraform/development/main.tf    |
| res-40        | terraform/hub/main.aztfexport.tf |
| vnet          | terraform/production/main.tf     |
| vnet          | terraform/qa/main.tf             |
| vnet          | terraform/staging/main.tf        |

#### azurerm_subnet (9)
| Resource Name | File                             |
|---------------|----------------------------------|
| subnet        | terraform/development/main.tf    |
| res-41        | terraform/hub/main.aztfexport.tf |
| res-42        | terraform/hub/main.aztfexport.tf |
| res-43        | terraform/hub/main.aztfexport.tf |
| res-44        | terraform/hub/main.aztfexport.tf |
| res-45        | terraform/hub/main.aztfexport.tf |
| subnet        | terraform/production/main.tf     |
| subnet        | terraform/qa/main.tf             |
| subnet        | terraform/staging/main.tf        |

#### azurerm_recovery_services_vault (2)
| Resource Name | File                                       |
|---------------|--------------------------------------------|
| rsvault       | terraform/development/recovery_services.tf |
| rsvault       | terraform/production/recovery_services.tf  |

#### azurerm_backup_policy_vm (4)
| Resource Name | File                                       |
|---------------|--------------------------------------------|
| daily_2300    | terraform/development/recovery_services.tf |
| daily_0700    | terraform/development/recovery_services.tf |
| daily_2300    | terraform/production/recovery_services.tf  |
| daily_0700    | terraform/production/recovery_services.tf  |

#### azurerm_resource_group (35)
| Resource Name        | File                                    |
|----------------------|-----------------------------------------|
| vnet_rg              | terraform/development/resourcegroups.tf |
| ops_rg               | terraform/development/resourcegroups.tf |
| apps_rg              | terraform/development/resourcegroups.tf |
| svcs_rg              | terraform/development/resourcegroups.tf |
| database_rg          | terraform/development/resourcegroups.tf |
| dataplatform_rg      | terraform/development/resourcegroups.tf |
| appplatform_rg       | terraform/development/resourcegroups.tf |
| invoicingplatform_rg | terraform/development/resourcegroups.tf |
| uipath_rg            | terraform/development/resourcegroups.tf |
| dataproc_rg          | terraform/development/resourcegroups.tf |
| res-0                | terraform/hub/main.aztfexport.tf        |
| vnet_rg              | terraform/production/resourcegroups.tf  |
| ops_rg               | terraform/production/resourcegroups.tf  |
| apps_rg              | terraform/production/resourcegroups.tf  |
| svcs_rg              | terraform/production/resourcegroups.tf  |
| database_rg          | terraform/production/resourcegroups.tf  |
| dataplatform_rg      | terraform/production/resourcegroups.tf  |
| appplatform_rg       | terraform/production/resourcegroups.tf  |
| nsalegalaffairs_rg   | terraform/production/resourcegroups.tf  |
| invoicingplatform_rg | terraform/production/resourcegroups.tf  |
| vnet_rg              | terraform/qa/resourcegroups.tf          |
| ops_rg               | terraform/qa/resourcegroups.tf          |
| apps_rg              | terraform/qa/resourcegroups.tf          |
| svcs_rg              | terraform/qa/resourcegroups.tf          |
| database_rg          | terraform/qa/resourcegroups.tf          |
| dataplatform_rg      | terraform/qa/resourcegroups.tf          |
| appplatform_rg       | terraform/qa/resourcegroups.tf          |
| invoicingplatform_rg | terraform/qa/resourcegroups.tf          |
| uipath_rg            | terraform/qa/resourcegroups.tf          |
| vnet_rg              | terraform/staging/resourcegroups.tf     |
| ops_rg               | terraform/staging/resourcegroups.tf     |
| apps_rg              | terraform/staging/resourcegroups.tf     |
| svcs_rg              | terraform/staging/resourcegroups.tf     |
| database_rg          | terraform/staging/resourcegroups.tf     |
| dataplatform_rg      | terraform/staging/resourcegroups.tf     |

#### azurerm_storage_queue (6)
| Resource Name                | File                              |
|------------------------------|-----------------------------------|
| idre                         | terraform/development/storage.tf  |
| idre_reselection_to_temporal | terraform/development/temporal.tf |
| idre                         | terraform/production/storage.tf   |
| idre_reselection_to_temporal | terraform/production/temporal.tf  |
| apps                         | terraform/qa/storage.tf           |
| idre                         | terraform/staging/storage.tf      |

#### azurerm_network_interface (39)
| Resource Name       | File                                        |
|---------------------|---------------------------------------------|
| temporal-nic        | terraform/development/temporal.tf           |
| airbyte-nic         | terraform/development/vm_airbyte.tf         |
| briefbuilder-nic    | terraform/development/vm_briefbuilder.tf    |
| dagster-nic         | terraform/development/vm_dagster.tf         |
| dataproc-nic        | terraform/development/vm_dataproc.tf        |
| monitoring-nic      | terraform/development/vm_monitoring.tf      |
| openmetadata-nic    | terraform/development/vm_openmetadata.tf    |
| taskautomations-nic | terraform/development/vm_taskautomations.tf |
| uipath-nic          | terraform/development/vm_uipath.tf          |
| webapps-nic         | terraform/development/vm_webapps.tf         |
| workflow_nic        | terraform/development/workflow.tf           |
| builder-nic         | terraform/development/deprecated/builder.tf |
| octopus-nic         | terraform/development/deprecated/octopus.tf |
| wireguard-nic       | terraform/development/unused/wireguard.tf   |
| builder-nic         | terraform/production/builder.tf             |
| octopus-nic         | terraform/production/octopus.tf             |
| temporal-nic        | terraform/production/temporal.tf            |
| airbyte-nic         | terraform/production/vm_airbyte.tf          |
| briefbuilder-nic    | terraform/production/vm_briefbuilder.tf     |
| dagster-nic         | terraform/production/vm_dagster.tf          |
| devops-nic          | terraform/production/vm_devops.tf           |
| monitoring-nic      | terraform/production/vm_monitoring.tf       |
| openmetadata-nic    | terraform/production/vm_openmetadata.tf     |
| taskautomations-nic | terraform/production/vm_taskautomations.tf  |
| webapps-nic         | terraform/production/vm_webapps.tf          |
| arbitdemo-nic       | terraform/qa/arbitdemo.tf                   |
| briefbuilder-nic    | terraform/qa/vm_briefbuilder.tf             |
| dagster-nic         | terraform/qa/vm_dagster.tf                  |
| monitoring-nic      | terraform/qa/vm_monitoring.tf               |
| taskautomations-nic | terraform/qa/vm_taskautomations.tf          |
| uipath-nic          | terraform/qa/vm_uipath.tf                   |
| webapps-nic         | terraform/qa/vm_webapps.tf                  |
| airbyte-nic         | terraform/qa/stage/vm_airbyte.tf            |
| airbyte-nic         | terraform/staging/vm_airbyte.tf             |
| briefbuilder-nic    | terraform/staging/vm_briefbuilder.tf        |
| dagster-nic         | terraform/staging/vm_dagster.tf             |
| openmetadata-nic    | terraform/staging/vm_openmetadata.tf        |
| taskautomations-nic | terraform/staging/vm_taskautomations.tf     |
| webapps-nic         | terraform/staging/vm_webapps.tf             |

#### azurerm_network_interface_security_group_association (39)
| Resource Name                       | File                                        |
|-------------------------------------|---------------------------------------------|
| temporal-nic-nsg-association        | terraform/development/temporal.tf           |
| airbyte-nic-nsg-association         | terraform/development/vm_airbyte.tf         |
| briefbuilder-nic-nsg-association    | terraform/development/vm_briefbuilder.tf    |
| dagster-nic-nsg-association         | terraform/development/vm_dagster.tf         |
| dataproc-nic-nsg-association        | terraform/development/vm_dataproc.tf        |
| monitoring-nic-nsg-association      | terraform/development/vm_monitoring.tf      |
| openmetadata-nic-nsg-association    | terraform/development/vm_openmetadata.tf    |
| taskautomations-nic-nsg-association | terraform/development/vm_taskautomations.tf |
| uipath-nic-nsg-association          | terraform/development/vm_uipath.tf          |
| webapps-nic-nsg-association         | terraform/development/vm_webapps.tf         |
| workflow-nic-nsg-association        | terraform/development/workflow.tf           |
| builder-nic-nsg-association         | terraform/development/deprecated/builder.tf |
| octopus-nic-nsg-association         | terraform/development/deprecated/octopus.tf |
| wireguard-nic-nsg-association       | terraform/development/unused/wireguard.tf   |
| builder-nic-nsg-association         | terraform/production/builder.tf             |
| octopus-nic-nsg-association         | terraform/production/octopus.tf             |
| temporal-nic-nsg-association        | terraform/production/temporal.tf            |
| airbyte-nic-nsg-association         | terraform/production/vm_airbyte.tf          |
| briefbuilder-nic-nsg-association    | terraform/production/vm_briefbuilder.tf     |
| dagster-nic-nsg-association         | terraform/production/vm_dagster.tf          |
| devops-nic-nsg-association          | terraform/production/vm_devops.tf           |
| monitoring-nic-nsg-association      | terraform/production/vm_monitoring.tf       |
| openmetadata-nic-nsg-association    | terraform/production/vm_openmetadata.tf     |
| taskautomations-nic-nsg-association | terraform/production/vm_taskautomations.tf  |
| webapps-nic-nsg-association         | terraform/production/vm_webapps.tf          |
| arbitdemo-nic-nsg-association       | terraform/qa/arbitdemo.tf                   |
| briefbuilder-nic-nsg-association    | terraform/qa/vm_briefbuilder.tf             |
| dagster-nic-nsg-association         | terraform/qa/vm_dagster.tf                  |
| monitoring-nic-nsg-association      | terraform/qa/vm_monitoring.tf               |
| taskautomations-nic-nsg-association | terraform/qa/vm_taskautomations.tf          |
| uipath-nic-nsg-association          | terraform/qa/vm_uipath.tf                   |
| webapps-nic-nsg-association         | terraform/qa/vm_webapps.tf                  |
| airbyte-nic-nsg-association         | terraform/qa/stage/vm_airbyte.tf            |
| airbyte-nic-nsg-association         | terraform/staging/vm_airbyte.tf             |
| briefbuilder-nic-nsg-association    | terraform/staging/vm_briefbuilder.tf        |
| dagster-nic-nsg-association         | terraform/staging/vm_dagster.tf             |
| openmetadata-nic-nsg-association    | terraform/staging/vm_openmetadata.tf        |
| taskautomations-nic-nsg-association | terraform/staging/vm_taskautomations.tf     |
| webapps-nic-nsg-association         | terraform/staging/vm_webapps.tf             |

#### azurerm_linux_virtual_machine (36)
| Resource Name      | File                                        |
|--------------------|---------------------------------------------|
| temporal-vm        | terraform/development/temporal.tf           |
| airbyte-vm         | terraform/development/vm_airbyte.tf         |
| briefbuilder_vm    | terraform/development/vm_briefbuilder.tf    |
| dagster-vm         | terraform/development/vm_dagster.tf         |
| monitoring-vm      | terraform/development/vm_monitoring.tf      |
| openmetadata-vm    | terraform/development/vm_openmetadata.tf    |
| taskautomations-vm | terraform/development/vm_taskautomations.tf |
| webapps-vm         | terraform/development/vm_webapps.tf         |
| workflow_vm        | terraform/development/workflow.tf           |
| builder-vm         | terraform/development/deprecated/builder.tf |
| octopus-vm         | terraform/development/deprecated/octopus.tf |
| wireguard-vm       | terraform/development/unused/wireguard.tf   |
| builder-vm         | terraform/production/builder.tf             |
| octopus-vm         | terraform/production/octopus.tf             |
| temporal-vm        | terraform/production/temporal.tf            |
| airbyte-vm         | terraform/production/vm_airbyte.tf          |
| briefbuilder_vm    | terraform/production/vm_briefbuilder.tf     |
| dagster-vm         | terraform/production/vm_dagster.tf          |
| devops-vm          | terraform/production/vm_devops.tf           |
| monitoring-vm      | terraform/production/vm_monitoring.tf       |
| openmetadata-vm    | terraform/production/vm_openmetadata.tf     |
| taskautomations-vm | terraform/production/vm_taskautomations.tf  |
| webapps-vm         | terraform/production/vm_webapps.tf          |
| arbitdemo-vm       | terraform/qa/arbitdemo.tf                   |
| briefbuilder_vm    | terraform/qa/vm_briefbuilder.tf             |
| dagster-vm         | terraform/qa/vm_dagster.tf                  |
| monitoring-vm      | terraform/qa/vm_monitoring.tf               |
| taskautomations-vm | terraform/qa/vm_taskautomations.tf          |
| webapps-vm         | terraform/qa/vm_webapps.tf                  |
| airbyte-vm         | terraform/qa/stage/vm_airbyte.tf            |
| airbyte-vm         | terraform/staging/vm_airbyte.tf             |
| briefbuilder_vm    | terraform/staging/vm_briefbuilder.tf        |
| dagster-vm         | terraform/staging/vm_dagster.tf             |
| openmetadata-vm    | terraform/staging/vm_openmetadata.tf        |
| taskautomations-vm | terraform/staging/vm_taskautomations.tf     |
| webapps-vm         | terraform/staging/vm_webapps.tf             |

#### azurerm_backup_protected_vm (5)
| Resource Name                  | File                                   |
|--------------------------------|----------------------------------------|
| temporal_vm_backup             | terraform/development/temporal.tf      |
| backup-protected-monitoring-vm | terraform/development/vm_monitoring.tf |
| backup-protected-webapps-vm    | terraform/development/vm_webapps.tf    |
| temporal_vm_backup             | terraform/production/temporal.tf       |
| backup-protected-monitoring-vm | terraform/production/vm_monitoring.tf  |

#### azurerm_postgresql_flexible_server_configuration (2)
| Resource Name         | File                              |
|-----------------------|-----------------------------------|
| appplatform_db_config | terraform/development/temporal.tf |
| appplatform_db_config | terraform/production/temporal.tf  |

#### azurerm_managed_disk (17)
| Resource Name             | File                                        |
|---------------------------|---------------------------------------------|
| briefbuilder_data_disk    | terraform/development/vm_briefbuilder.tf    |
| dagster_usercode_disk     | terraform/development/vm_dagster.tf         |
| dataproc_usercode_disk    | terraform/development/vm_dataproc.tf        |
| taskautomations_data_disk | terraform/development/vm_taskautomations.tf |
| uipath_usercode_disk      | terraform/development/vm_uipath.tf          |
| builder_data_disk         | terraform/production/builder.tf             |
| octopus_data_disk         | terraform/production/octopus.tf             |
| briefbuilder_data_disk    | terraform/production/vm_briefbuilder.tf     |
| dagster_usercode_disk     | terraform/production/vm_dagster.tf          |
| taskautomations_data_disk | terraform/production/vm_taskautomations.tf  |
| briefbuilder_data_disk    | terraform/qa/vm_briefbuilder.tf             |
| dagster_usercode_disk     | terraform/qa/vm_dagster.tf                  |
| taskautomations_data_disk | terraform/qa/vm_taskautomations.tf          |
| uipath_usercode_disk      | terraform/qa/vm_uipath.tf                   |
| briefbuilder_data_disk    | terraform/staging/vm_briefbuilder.tf        |
| dagster_usercode_disk     | terraform/staging/vm_dagster.tf             |
| taskautomations_data_disk | terraform/staging/vm_taskautomations.tf     |

#### azurerm_virtual_machine_data_disk_attachment (17)
| Resource Name                        | File                                        |
|--------------------------------------|---------------------------------------------|
| briefbuilder_data_disk_attachment    | terraform/development/vm_briefbuilder.tf    |
| dagster_usercode_disk_attachment     | terraform/development/vm_dagster.tf         |
| dataproc_usercode_disk_attachment    | terraform/development/vm_dataproc.tf        |
| taskautomations_data_disk_attachment | terraform/development/vm_taskautomations.tf |
| uipath_usercode_disk_attachment      | terraform/development/vm_uipath.tf          |
| builder_data_disk_attachment         | terraform/production/builder.tf             |
| octopus_data_disk_attachment         | terraform/production/octopus.tf             |
| briefbuilder_data_disk_attachment    | terraform/production/vm_briefbuilder.tf     |
| dagster_usercode_disk_attachment     | terraform/production/vm_dagster.tf          |
| taskautomations_data_disk_attachment | terraform/production/vm_taskautomations.tf  |
| briefbuilder_data_disk_attachment    | terraform/qa/vm_briefbuilder.tf             |
| dagster_usercode_disk_attachment     | terraform/qa/vm_dagster.tf                  |
| taskautomations_data_disk_attachment | terraform/qa/vm_taskautomations.tf          |
| uipath_usercode_disk_attachment      | terraform/qa/vm_uipath.tf                   |
| briefbuilder_data_disk_attachment    | terraform/staging/vm_briefbuilder.tf        |
| dagster_usercode_disk_attachment     | terraform/staging/vm_dagster.tf             |
| taskautomations_data_disk_attachment | terraform/staging/vm_taskautomations.tf     |

#### random_password (3)
| Resource Name  | File                                 |
|----------------|--------------------------------------|
| dataproc_admin | terraform/development/vm_dataproc.tf |
| uipath_admin   | terraform/development/vm_uipath.tf   |
| uipath_admin   | terraform/qa/vm_uipath.tf            |

#### azurerm_windows_virtual_machine (3)
| Resource Name | File                                 |
|---------------|--------------------------------------|
| dataproc-vm   | terraform/development/vm_dataproc.tf |
| uipath-vm     | terraform/development/vm_uipath.tf   |
| uipath-vm     | terraform/qa/vm_uipath.tf            |

#### azurerm_virtual_machine_extension (6)
| Resource Name            | File                                 |
|--------------------------|--------------------------------------|
| configure_dataproc_winrm | terraform/development/vm_dataproc.tf |
| dataproc_aad_login       | terraform/development/vm_dataproc.tf |
| configure_uipath_winrm   | terraform/development/vm_uipath.tf   |
| uipath_aad_login         | terraform/development/vm_uipath.tf   |
| configure_uipath_winrm   | terraform/qa/vm_uipath.tf            |
| aad_login                | terraform/qa/vm_uipath.tf            |

#### azurerm_storage_account_static_website (1)
| Resource Name           | File                              |
|-------------------------|-----------------------------------|
| workflow_static_website | terraform/development/workflow.tf |

#### azurerm_cdn_profile (1)
| Resource Name        | File                              |
|----------------------|-----------------------------------|
| workflow_cdn_profile | terraform/development/workflow.tf |

#### azurerm_cdn_endpoint (1)
| Resource Name | File                              |
|---------------|-----------------------------------|
| workflow_cdn  | terraform/development/workflow.tf |

#### azurerm_redis_cache (1)
| Resource Name        | File                              |
|----------------------|-----------------------------------|
| workflow_redis_cache | terraform/development/workflow.tf |

#### azurerm_mssql_server (5)
| Resource Name       | File                                        |
|---------------------|---------------------------------------------|
| workflow_sql_server | terraform/development/workflow.tf           |
| sql_server          | terraform/development/deprecated/arbitv2.tf |
| octopus_sqlserver   | terraform/development/deprecated/octopus.tf |
| octopus_sqlserver   | terraform/production/octopus.tf             |
| arbitdemo_sqlserver | terraform/qa/arbitdemo.tf                   |

#### azurerm_mssql_database (6)
| Resource Name        | File                                        |
|----------------------|---------------------------------------------|
| workflow_sql_db      | terraform/development/workflow.tf           |
| workflow_logs_sql_db | terraform/development/workflow.tf           |
| octopus_db           | terraform/development/deprecated/octopus.tf |
| octopus_db           | terraform/production/octopus.tf             |
| arbitdemo_db         | terraform/qa/arbitdemo.tf                   |
| idrsupport_db        | terraform/qa/arbitdemo.tf                   |

#### azurerm_lb (1) 
| Resource Name | File                              |
|---------------|-----------------------------------|
| workflow_lb   | terraform/development/workflow.tf |

#### azurerm_lb_backend_address_pool (1)
| Resource Name       | File                              |
|---------------------|-----------------------------------|
| workflow_lb_backend | terraform/development/workflow.tf |

#### azurerm_lb_probe (1)
| Resource Name     | File                              |
|-------------------|-----------------------------------|
| workflow_lb_probe | terraform/development/workflow.tf |

#### azurerm_lb_rule (1)
| Resource Name    | File                              |
|------------------|-----------------------------------|
| workflow_lb_rule | terraform/development/workflow.tf |

#### azurerm_servicebus_namespace (1)
| Resource Name | File                                        |
|---------------|---------------------------------------------|
| servicebus    | terraform/development/deprecated/arbitv2.tf |

#### azurerm_servicebus_namespace_authorization_rule (1)
| Resource Name                | File                                        |
|------------------------------|---------------------------------------------|
| benchmark_authorization_rule | terraform/development/deprecated/arbitv2.tf |

#### azurerm_cosmosdb_account (1)
| Resource Name | File                                        |
|---------------|---------------------------------------------|
| cosmosdb      | terraform/development/deprecated/arbitv2.tf |

#### azurerm_cosmosdb_sql_database (1)
| Resource Name        | File                                        |
|----------------------|---------------------------------------------|
| benchmark_service_db | terraform/development/deprecated/arbitv2.tf |

#### azurerm_cosmosdb_sql_container (1)
| Resource Name        | File                                        |
|----------------------|---------------------------------------------|
| benchmarks_container | terraform/development/deprecated/arbitv2.tf |

#### azurerm_sql_database (1)
| Resource Name | File                                        |
|---------------|---------------------------------------------|
| managed_db    | terraform/development/deprecated/arbitv2.tf |

#### azurerm_bastion_host (1)
| Resource Name | File                                    |
|---------------|-----------------------------------------|
| bastion       | terraform/development/unused/bastion.tf |

#### azurerm_firewall (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-1         | terraform/hub/main.aztfexport.tf |

#### azurerm_virtual_network_gateway_connection (3)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-2         | terraform/hub/main.aztfexport.tf |
| res-3         | terraform/hub/main.aztfexport.tf |
| res-4         | terraform/hub/main.aztfexport.tf |

#### azurerm_private_dns_resolver_dns_forwarding_ruleset (2)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-5         | terraform/hub/main.aztfexport.tf |
| res-8         | terraform/hub/main.aztfexport.tf |

#### azurerm_private_dns_resolver_forwarding_rule (3)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-6         | terraform/hub/main.aztfexport.tf |
| res-7         | terraform/hub/main.aztfexport.tf |
| res-9         | terraform/hub/main.aztfexport.tf |

#### azurerm_private_dns_resolver_virtual_network_link (4)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-10        | terraform/hub/main.aztfexport.tf |
| res-11        | terraform/hub/main.aztfexport.tf |
| res-12        | terraform/hub/main.aztfexport.tf |
| res-13        | terraform/hub/main.aztfexport.tf |

#### azurerm_private_dns_resolver (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-14        | terraform/hub/main.aztfexport.tf |

#### azurerm_private_dns_resolver_inbound_endpoint (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-15        | terraform/hub/main.aztfexport.tf |

#### azurerm_private_dns_resolver_outbound_endpoint (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-16        | terraform/hub/main.aztfexport.tf |

#### azurerm_firewall_policy (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-17        | terraform/hub/main.aztfexport.tf |

#### azurerm_firewall_policy_rule_collection_group (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-18        | terraform/hub/main.aztfexport.tf |

#### azurerm_local_network_gateway (3)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-19        | terraform/hub/main.aztfexport.tf |
| res-20        | terraform/hub/main.aztfexport.tf |
| res-21        | terraform/hub/main.aztfexport.tf |

#### azurerm_virtual_network_gateway (1)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-39        | terraform/hub/main.aztfexport.tf |

#### azurerm_virtual_network_peering (10)
| Resource Name | File                             |
|---------------|----------------------------------|
| res-47        | terraform/hub/main.aztfexport.tf |
| res-48        | terraform/hub/main.aztfexport.tf |
| res-49        | terraform/hub/main.aztfexport.tf |
| res-50        | terraform/hub/main.aztfexport.tf |
| res-51        | terraform/hub/main.aztfexport.tf |
| res-52        | terraform/hub/main.aztfexport.tf |
| res-53        | terraform/hub/main.aztfexport.tf |
| res-54        | terraform/hub/main.aztfexport.tf |
| res-55        | terraform/hub/main.aztfexport.tf |
| res-56        | terraform/hub/main.aztfexport.tf |

#### azurerm_mssql_virtual_network_rule (2)
| Resource Name             | File                            |
|---------------------------|---------------------------------|
| octopus_db_network_rule   | terraform/production/octopus.tf |
| arbitdemo_db_network_rule | terraform/qa/arbitdemo.tf       |