# CloudWatch Monitoring and Alarms for Category Migration Lambda

# CloudWatch Alarm for Lambda errors
resource "aws_cloudwatch_metric_alarm" "lambda_errors" {
  alarm_name          = "${var.function_name}-errors"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "Errors"
  namespace           = "AWS/Lambda"
  period              = "300"
  statistic           = "Sum"
  threshold           = "0"
  alarm_description   = "This metric monitors lambda errors"
  
  dimensions = {
    FunctionName = aws_lambda_function.category_migration.function_name
  }
  
  alarm_actions = var.environment == "production" ? [aws_sns_topic.lambda_alerts.arn] : []
  
  tags = local.common_tags
}

# CloudWatch Alarm for Lambda duration
resource "aws_cloudwatch_metric_alarm" "lambda_duration" {
  alarm_name          = "${var.function_name}-duration"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "Duration"
  namespace           = "AWS/Lambda"
  period              = "300"
  statistic           = "Average"
  threshold           = "600000" # 10 minutes in milliseconds
  alarm_description   = "This metric monitors lambda duration"
  
  dimensions = {
    FunctionName = aws_lambda_function.category_migration.function_name
  }
  
  alarm_actions = var.environment == "production" ? [aws_sns_topic.lambda_alerts.arn] : []
  
  tags = local.common_tags
}

# CloudWatch Alarm for Lambda throttles
resource "aws_cloudwatch_metric_alarm" "lambda_throttles" {
  alarm_name          = "${var.function_name}-throttles"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "Throttles"
  namespace           = "AWS/Lambda"
  period              = "300"
  statistic           = "Sum"
  threshold           = "0"
  alarm_description   = "This metric monitors lambda throttles"
  
  dimensions = {
    FunctionName = aws_lambda_function.category_migration.function_name
  }
  
  alarm_actions = var.environment == "production" ? [aws_sns_topic.lambda_alerts.arn] : []
  
  tags = local.common_tags
}

# SNS Topic for alerts (production only)
resource "aws_sns_topic" "lambda_alerts" {
  count = var.environment == "production" ? 1 : 0
  name  = "${var.function_name}-alerts"
  
  tags = local.common_tags
}

# CloudWatch Dashboard
resource "aws_cloudwatch_dashboard" "lambda_dashboard" {
  dashboard_name = "${var.function_name}-dashboard"
  
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
            ["AWS/Lambda", "Invocations", "FunctionName", aws_lambda_function.category_migration.function_name],
            [".", "Errors", ".", "."],
            [".", "Duration", ".", "."],
            [".", "Throttles", ".", "."]
          ]
          view    = "timeSeries"
          stacked = false
          region  = var.aws_region
          title   = "Lambda Function Metrics"
          period  = 300
        }
      }
    ]
  })
  
  tags = local.common_tags
}
