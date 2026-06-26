variable "location" { default = "italynorth" }
variable "environment_name" { default = "ledgercore" }

variable "db_password" {
  type        = string
  sensitive   = true
}
variable "redis_connection" {
  type        = string
  sensitive   = true
}
variable "rabbitmq_host" {
  type        = string
  sensitive   = true
}
variable "jwt_secret" {
  type      = string
  sensitive = true
}
variable "jwt_issuer" { default = "italynorth" }
variable "jwt_audience" { default = "italynorth" }
