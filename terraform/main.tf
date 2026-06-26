# Global Unique Suffix
resource "random_integer" "suffix" {
  min = 1000
  max = 9999
}

# 1. Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "${var.environment_name}-rg"
  location = var.location
}

# 2. Azure SQL (Serverless Tier - Scales to Zero Compute)
resource "azurerm_mssql_server" "sql_server" {
  name                         = "${var.environment_name}-sql-${random_integer.suffix.result}"
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = "sa_admin"
  administrator_login_password = var.db_password
}

resource "azurerm_mssql_firewall_rule" "allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "sqldb" {
  name      = "LedgerCoreDb"
  server_id = azurerm_mssql_server.sql_server.id
  sku_name  = "GP_S_Gen5_1" # Serverless SKU
  min_capacity = 0.5
  auto_pause_delay_in_minutes = 30 # Pauses billing after 30 minutes of inactivity

  tags = { Environment = "Production", CostCenter = "ZeroCost" }
}

# 3. Container Registry (Basic SKU)
resource "azurerm_container_registry" "acr" {
  name                = "ledgercoreacr${random_integer.suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = true
}

# 4. Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "law" {
  name                = "${var.environment_name}-law"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# 5. Container Apps Environment
resource "azurerm_container_app_environment" "aca_env" {
  name                       = "${var.environment_name}-env"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id
}

# 6. Azure Container App (Consumption Tier)
resource "azurerm_container_app" "api" {
  name                         = "${var.environment_name}-api"
  container_app_environment_id = azurerm_container_app_environment.aca_env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  template {
    min_replicas = 0 # ZERO COST ENFORCEMENT
    max_replicas = 2

    container {
      name   = "ledger-api"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest" # Placeholder image
      cpu    = 0.25
      memory = "0.5Gi"

      env { 
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production" 
      }
      env { 
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "db-connection" 
      }
      env { 
        name        = "Redis__Configuration"
        secret_name = "redis-connection" 
      }
      env { 
        name        = "RabbitMQ__Host"
        secret_name = "rabbitmq-host" 
      }
      env { 
        name        = "Jwt__Secret"
        secret_name = "jwt-secret" 
      }
      env { 
        name  = "Jwt__Issuer"
        value = var.jwt_issuer 
      }
      env { 
        name  = "Jwt__Audience"
        value = var.jwt_audience 
      }
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 8080
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  secret {
    name  = "db-connection"
    value = "Server=tcp:${azurerm_mssql_server.sql_server.fully_qualified_domain_name},1433;Initial Catalog=LedgerCoreDb;Persist Security Info=False;User ID=${azurerm_mssql_server.sql_server.administrator_login};Password=${var.db_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
  secret { 
    name  = "redis-connection"
    value = var.redis_connection 
  }
  secret { 
    name  = "rabbitmq-host"
    value = var.rabbitmq_host 
  }
  secret { 
    name  = "jwt-secret"
    value = var.jwt_secret 
  }
}
