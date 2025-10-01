variable "lambda_name" {
  description = "Name of the lambda function (without prefix)"
  type        = string
}

variable "handler" {
  description = "Lambda function handler"
  type        = string
}

variable "runtime" {
  description = "Lambda runtime"
  type        = string
  default     = "dotnet8"
}

variable "timeout" {
  description = "Lambda timeout in seconds"
  type        = number
  default     = 900
}

variable "memory_size" {
  description = "Lambda memory size in MB"
  type        = number
  default     = 512
}

variable "environment_variables" {
  description = "Environment variables for the lambda function"
  type        = map(string)
  default     = {}
}

variable "schedule_expression" {
  description = "CloudWatch Events schedule expression (optional)"
  type        = string
  default     = null
}

variable "log_retention_days" {
  description = "CloudWatch log retention days"
  type        = number
  default     = 30
}

variable "workspace" {
  description = "Terraform workspace/environment name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID for lambda (optional)"
  type        = string
  default     = null
}

variable "subnet_ids" {
  description = "Subnet IDs for lambda (optional)"
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

variable "vault_addr" {
  type    = map(string)
  default = {}
}

variable "vault_prefix" {
  type    = map(string)
  default = {}
}

variable "lambda_log_level" {
  type = map(string)
}
