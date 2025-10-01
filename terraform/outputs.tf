# Outputs for Category Migration Lambda Infrastructure

output "lambda_function_name" {
  description = "Name of the Lambda function"
  value       = aws_lambda_function.category_migration.function_name
}

output "lambda_function_arn" {
  description = "ARN of the Lambda function"
  value       = aws_lambda_function.category_migration.arn
}

output "lambda_function_invoke_arn" {
  description = "Invoke ARN of the Lambda function"
  value       = aws_lambda_function.category_migration.invoke_arn
}

output "lambda_role_arn" {
  description = "ARN of the Lambda execution role"
  value       = aws_iam_role.lambda_role.arn
}

output "cloudwatch_log_group_name" {
  description = "Name of the CloudWatch log group"
  value       = aws_cloudwatch_log_group.lambda_logs.name
}

output "cloudwatch_log_group_arn" {
  description = "ARN of the CloudWatch log group"
  value       = aws_cloudwatch_log_group.lambda_logs.arn
}

output "cloudwatch_dashboard_url" {
  description = "URL of the CloudWatch dashboard"
  value       = "https://${var.aws_region}.console.aws.amazon.com/cloudwatch/home?region=${var.aws_region}#dashboards:name=${aws_cloudwatch_dashboard.lambda_dashboard.dashboard_name}"
}

output "sns_topic_arn" {
  description = "ARN of the SNS topic for alerts (production only)"
  value       = var.environment == "production" ? aws_sns_topic.lambda_alerts[0].arn : null
}

# Example usage outputs
output "invoke_lambda_example" {
  description = "Example command to invoke the Lambda function"
  value       = "aws lambda invoke --function-name ${aws_lambda_function.category_migration.function_name} --payload '{\"type\":\"all\",\"dryRun\":true}' --cli-binary-format raw-in-base64-out response.json"
}

output "view_logs_example" {
  description = "Example command to view Lambda logs"
  value       = "aws logs tail /aws/lambda/${aws_lambda_function.category_migration.function_name} --follow"
}
