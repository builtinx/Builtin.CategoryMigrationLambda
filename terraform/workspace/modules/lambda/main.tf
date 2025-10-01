# Reusable Lambda Module
# This module creates a complete lambda function with all necessary resources

locals {
  function_name  = "category-migration-${var.lambda_name}"
  log_group_name = "/aws/lambda/${local.function_name}"
}

# Lambda Function
resource "aws_lambda_function" "this" {
  filename         = data.archive_file.placeholder-code.output_path
  function_name    = local.function_name
  role             = aws_iam_role.lambda_role.arn
  handler          = var.handler
  runtime          = var.runtime
  timeout          = var.timeout
  memory_size      = var.memory_size
  source_code_hash = data.archive_file.placeholder-code.output_base64sha256

  dynamic "environment" {
    for_each = length(var.environment_variables) > 0 ? [1] : []
    content {
      variables = var.environment_variables
    }
  }

  dynamic "vpc_config" {
    for_each = var.vpc_id != null ? [1] : []
    content {
      subnet_ids         = var.subnet_ids
      security_group_ids = [aws_security_group.lambda[0].id]
    }
  }

  tags = merge(var.tags, {
    Name = local.function_name
  })
}

# CloudWatch Events Rule for scheduled execution
resource "aws_cloudwatch_event_rule" "schedule" {
  count               = var.schedule_expression != null ? 1 : 0
  name                = "${local.function_name}-cloudwatch-schedule-${var.workspace}"
  description         = "CloudWatch Events rule to trigger ${local.function_name} Lambda on schedule"
  schedule_expression = var.schedule_expression
  state               = "ENABLED"

  tags = merge(var.tags, {
    Name        = "${local.function_name}-cloudwatch-schedule"
    TriggerType = "CloudWatchEvents"
  })
}

# CloudWatch Events Target to invoke Lambda
resource "aws_cloudwatch_event_target" "target" {
  count     = var.schedule_expression != null ? 1 : 0
  rule      = aws_cloudwatch_event_rule.schedule[0].name
  target_id = "${title(var.lambda_name)}CloudWatchTarget"
  arn       = aws_lambda_function.this.arn

  # Optional: Add custom event data
  input_transformer {
    input_paths = {
      eventTime = "$.time"
    }
    input_template = jsonencode({
      source       = "cloudwatch-events"
      environment  = var.workspace
      trigger_type = "scheduled"
      timestamp    = "<eventTime>"
    })
  }
}

# Lambda permission to allow CloudWatch Events to invoke the function
resource "aws_lambda_permission" "allow_cloudwatch_events" {
  count         = var.schedule_expression != null ? 1 : 0
  statement_id  = "AllowExecutionFromCloudWatchEvents"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.this.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.schedule[0].arn
}

# CloudWatch Log Group for Lambda function
resource "aws_cloudwatch_log_group" "logs" {
  name              = local.log_group_name
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${local.function_name}-logs"
  })
}

# IAM Role for Lambda
resource "aws_iam_role" "lambda_role" {
  name = "${local.function_name}-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

# Basic execution role policy
resource "aws_iam_role_policy_attachment" "lambda_basic" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# VPC access role policy (only if VPC is configured)
resource "aws_iam_role_policy_attachment" "lambda_vpc" {
  count      = var.vpc_id != null ? 1 : 0
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# DynamoDB access policy for category migration
resource "aws_iam_role_policy" "dynamodb_access" {
  name = "${local.function_name}-dynamodb-policy"
  role = aws_iam_role.lambda_role.name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem",
          "dynamodb:UpdateItem",
          "dynamodb:DeleteItem",
          "dynamodb:Query",
          "dynamodb:Scan",
          "dynamodb:BatchGetItem",
          "dynamodb:BatchWriteItem"
        ]
        Resource = [
          "arn:aws:dynamodb:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:table/*"
        ]
      }
    ]
  })
}

# CloudWatch metrics policy for custom metrics
resource "aws_iam_role_policy" "cloudwatch_metrics" {
  name = "${local.function_name}-cloudwatch-metrics-policy"
  role = aws_iam_role.lambda_role.name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "cloudwatch:PutMetricData"
        ]
        Resource = "*"
        Condition = {
          StringEquals = {
            "cloudwatch:namespace" = "CategoryMigration"
          }
        }
      }
    ]
  })
}

# Security Group for Lambda (only if VPC is configured)
resource "aws_security_group" "lambda" {
  count       = var.vpc_id != null ? 1 : 0
  name_prefix = "${local.function_name}-"
  vpc_id      = var.vpc_id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, {
    Name = "${local.function_name}-sg"
  })
}

# Placeholder archive for Lambda deployment
data "archive_file" "placeholder-code" {
  type        = "zip"
  output_path = "${path.module}/placeholder.zip"

  source {
    content  = "Placeholder"
    filename = "placeholder.txt"
  }
}

# Data sources
data "aws_region" "current" {}
data "aws_caller_identity" "current" {}

