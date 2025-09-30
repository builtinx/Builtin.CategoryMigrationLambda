# Variables for Category Migration Lambda Infrastructure

variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "us-west-2"
}

variable "environment" {
  description = "Environment name (dev, staging, production)"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "staging", "production"], var.environment)
    error_message = "Environment must be one of: dev, staging, production."
  }
}

variable "function_name" {
  description = "Name of the Lambda function"
  type        = string
  default     = "category-migration-lambda"
}

variable "runtime" {
  description = "Lambda runtime"
  type        = string
  default     = "dotnet8"
}

variable "handler" {
  description = "Lambda handler"
  type        = string
  default     = "CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler"
}

variable "timeout" {
  description = "Lambda timeout in seconds"
  type        = number
  default     = 900
}

variable "memory_size" {
  description = "Lambda memory size in MB"
  type        = number
  default     = 1024
}

variable "dynamodb_table_name" {
  description = "Name of the DynamoDB table to access"
  type        = string
  default     = "Users"
}

variable "log_retention_days" {
  description = "CloudWatch log retention in days"
  type        = number
  default     = 14
}

variable "enable_xray_tracing" {
  description = "Enable X-Ray tracing for the Lambda function"
  type        = bool
  default     = true
}

variable "reserved_concurrency" {
  description = "Reserved concurrency for the Lambda function"
  type        = number
  default     = 1
}
