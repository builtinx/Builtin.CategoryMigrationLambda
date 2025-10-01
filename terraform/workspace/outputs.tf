# Outputs for Category Migration Lambda Infrastructure

output "lambda_function_name" {
  description = "Name of the Lambda function"
  value       = module.category_migration_lambda.lambda_function_name
}

output "lambda_function_arn" {
  description = "ARN of the Lambda function"
  value       = module.category_migration_lambda.lambda_function_arn
}

output "lambda_function_invoke_arn" {
  description = "Invoke ARN of the Lambda function"
  value       = module.category_migration_lambda.lambda_function_invoke_arn
}

output "lambda_role_arn" {
  description = "ARN of the Lambda execution role"
  value       = module.category_migration_lambda.lambda_role_arn
}

output "cloudwatch_log_group_name" {
  description = "Name of the CloudWatch log group"
  value       = module.category_migration_lambda.cloudwatch_log_group_name
}

output "cloudwatch_log_group_arn" {
  description = "ARN of the CloudWatch log group"
  value       = module.category_migration_lambda.cloudwatch_log_group_name
}

output "cloudwatch_dashboard_url" {
  description = "URL of the CloudWatch dashboard"
  value       = "https://us-west-2.console.aws.amazon.com/cloudwatch/home?region=us-west-2#dashboards:name=${aws_cloudwatch_dashboard.lambda_dashboard.dashboard_name}"
}

output "sns_topic_arn" {
  description = "ARN of the SNS topic for alerts (production only)"
  value       = local.workspace == "prod" ? aws_sns_topic.lambda_alerts[0].arn : null
}

# Example usage outputs
output "invoke_lambda_example" {
  description = "Example command to invoke the Lambda function"
  value       = "aws lambda invoke --function-name ${module.category_migration_lambda.lambda_function_name} --payload '{\"type\":\"all\",\"dryRun\":true}' --cli-binary-format raw-in-base64-out response.json"
}

output "view_logs_example" {
  description = "Example command to view Lambda logs"
  value       = "aws logs tail /aws/lambda/${module.category_migration_lambda.lambda_function_name} --follow"
}
