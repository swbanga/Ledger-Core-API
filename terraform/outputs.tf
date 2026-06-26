output "api_endpoint" {
  value = "https://${azurerm_container_app.api.latest_revision_fqdn}"
}
output "acr_login_server" {
  value = azurerm_container_registry.acr.login_server
}
output "acr_username" {
  value = azurerm_container_registry.acr.admin_username
}
