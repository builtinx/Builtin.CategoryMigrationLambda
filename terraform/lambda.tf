# Lambda Function for Category Migration

# Create the Lambda function
resource "aws_lambda_function" "category_migration" {
  function_name = var.function_name
  role          = aws_iam_role.lambda_role.arn
  handler       = var.handler
  runtime       = var.runtime
  timeout       = var.timeout
  memory_size   = var.memory_size
  
  # The deployment package will be provided by CI/CD
  filename         = "dummy.zip"
  source_code_hash = data.archive_file.dummy_zip.output_base64sha256
  
  description = "Category and subcategory migration for job preferences"
  
  # Environment variables
  environment {
    variables = {
      DOTNET_ENVIRONMENT = var.environment
      DYNAMODB_TABLE_NAME = var.dynamodb_table_name
    }
  }
  
  # Tracing configuration
  tracing_config {
    mode = var.enable_xray_tracing ? "Active" : "PassThrough"
  }
  
  # Reserved concurrency
  reserved_concurrent_executions = var.reserved_concurrency
  
  tags = local.common_tags
  
  depends_on = [
    aws_iam_role_policy_attachment.lambda_basic_execution,
    aws_iam_role_policy.dynamodb_access,
    aws_cloudwatch_log_group.lambda_logs
  ]
}

# Dummy zip file for initial deployment
data "archive_file" "dummy_zip" {
  type        = "zip"
  output_path = "dummy.zip"
  source {
    content  = "dummy"
    filename = "dummy.txt"
  }
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "lambda_logs" {
  name              = "/aws/lambda/${var.function_name}"
  retention_in_days = var.log_retention_days
  
  tags = local.common_tags
}

# Lambda permission for CloudWatch Logs
resource "aws_lambda_permission" "allow_cloudwatch_logs" {
  statement_id  = "AllowExecutionFromCloudWatch"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.category_migration.function_name
  principal     = "logs.amazonaws.com"
  source_arn    = "${aws_cloudwatch_log_group.lambda_logs.arn}:*"
}
