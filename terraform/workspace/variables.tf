variable "workspace" {
  type    = string
  default = ""
}


variable "lambda_log_level" {
  type = map(string)
}

variable "schedule_expression" {
  type        = map(string)
  description = "CloudWatch Events schedule expression for Lambda function triggers"
}

variable "log_retention_days" {
  type        = map(number)
  description = "CloudWatch log retention days for Lambda function logs"
}
