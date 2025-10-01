# CloudWatch Monitoring and Alarms for Category Migration Lambda

# CloudWatch Alarm for Lambda errors
resource "aws_cloudwatch_metric_alarm" "lambda_errors" {
  alarm_name          = "category-migration-lambda-errors"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "Errors"
  namespace           = "AWS/Lambda"
  period              = "300"
  statistic           = "Sum"
  threshold           = "0"
  alarm_description   = "This metric monitors lambda errors"
  dimensions = {
    FunctionName = module.category_migration_lambda.lambda_function_name
  }
  alarm_actions = local.workspace == "prod" ? [aws_sns_topic.lambda_alerts[0].arn] : []
  tags = {
    Environment  = local.workspace
    Service      = "category-migration"
    Component    = "category-migration"
    map-migrated = "migGMI64LOT81"
  }
}

# CloudWatch Alarm for Lambda duration
resource "aws_cloudwatch_metric_alarm" "lambda_duration" {
  alarm_name          = "category-migration-lambda-duration"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "Duration"
  namespace           = "AWS/Lambda"
  period              = "300"
  statistic           = "Average"
  threshold           = "600000" # 10 minutes in milliseconds
  alarm_description   = "This metric monitors lambda duration"
  dimensions = {
    FunctionName = module.category_migration_lambda.lambda_function_name
  }
  alarm_actions = local.workspace == "prod" ? [aws_sns_topic.lambda_alerts[0].arn] : []
  tags = {
    Environment  = local.workspace
    Service      = "category-migration"
    Component    = "category-migration"
    map-migrated = "migGMI64LOT81"
  }
}

# CloudWatch Alarm for Lambda throttles
resource "aws_cloudwatch_metric_alarm" "lambda_throttles" {
  alarm_name          = "category-migration-lambda-throttles"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "Throttles"
  namespace           = "AWS/Lambda"
  period              = "300"
  statistic           = "Sum"
  threshold           = "0"
  alarm_description   = "This metric monitors lambda throttles"
  dimensions = {
    FunctionName = module.category_migration_lambda.lambda_function_name
  }
  alarm_actions = local.workspace == "prod" ? [aws_sns_topic.lambda_alerts[0].arn] : []
  tags = {
    Environment  = local.workspace
    Service      = "category-migration"
    Component    = "category-migration"
    map-migrated = "migGMI64LOT81"
  }
}

# SNS Topic for alerts (production only)
resource "aws_sns_topic" "lambda_alerts" {
  count = local.workspace == "prod" ? 1 : 0
  name  = "category-migration-lambda-alerts"
  tags = {
    Environment  = local.workspace
    Service      = "category-migration"
    Component    = "category-migration"
    map-migrated = "migGMI64LOT81"
  }
}

# CloudWatch Dashboard
resource "aws_cloudwatch_dashboard" "lambda_dashboard" {
  dashboard_name = "category-migration-lambda-dashboard"
  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6
        properties = {
          metrics = [
            ["AWS/Lambda", "Invocations", "FunctionName", module.category_migration_lambda.lambda_function_name],
            [".", "Errors", ".", "."],
            [".", "Duration", ".", "."],
            [".", "Throttles", ".", "."]
          ]
          view    = "timeSeries"
          stacked = false
          region  = "us-west-2"
          title   = "Lambda Function Metrics"
          period  = 300
        }
      }
    ]
  })
}
