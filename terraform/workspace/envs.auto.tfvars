# Environment-specific variables

lambda_log_level = {
  default    = "Information"
  develop    = "Information"
  staging    = "Information"
  production = "Warning"
  prod       = "Warning"
}

schedule_expression = {
  default    = null
  develop    = null
  staging    = null
  production = null
  prod       = null
}

log_retention_days = {
  default    = 7
  develop    = 7
  staging    = 14
  production = 30
  prod       = 30
}
