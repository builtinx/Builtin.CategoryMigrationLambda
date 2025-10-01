# Environment-specific variables

aws_region = "us-east-1"

vault_addr = {
  "develop" = "https://vault-develop.builtin.com"
  "staging" = "https://vault-staging.builtin.com"
  "prod"    = "https://vault-prod.builtin.com"
}

vault_prefix = {
  "develop" = "terraform-develop"
  "staging" = "terraform-staging"
  "prod"    = "terraform-prod"
}

lambda_log_level = {
  default    = "Information"
  develop    = "Information"
  staging    = "Information"
  production = "Warning"
}

schedule_expression = {
  default    = "rate(5 minutes)"
  develop    = "rate(5 minutes)"
  staging    = "rate(15 minutes)"
  production = "rate(1 hour)"
}

log_retention_days = {
  default    = 7
  develop    = 7
  staging    = 14
  production = 30
}
