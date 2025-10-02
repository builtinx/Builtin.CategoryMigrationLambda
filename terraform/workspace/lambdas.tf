# Main Lambda Functions Configuration
# This file manages the Category Migration Lambda function

# Category Migration Lambda Function
module "category_migration_lambda" {
  source = "./modules/lambda"

  lambda_name = "lambda-${local.workspace}"
  handler     = "CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler"
  runtime     = "dotnet8"
  timeout     = 900
  memory_size = 1024

  environment_variables = {
    DOTNET_ENVIRONMENT  = local.workspace
    LOG_LEVEL           = var.lambda_log_level[local.workspace]
    DynamoDB__TableName = "Users"
  }

  schedule_expression = var.schedule_expression[local.workspace]
  log_retention_days  = var.log_retention_days[local.workspace]
  workspace           = local.workspace
  lambda_log_level    = var.lambda_log_level

  tags = {
    Environment  = local.workspace
    Service      = "category-migration"
    Component    = "category-migration"
    map-migrated = "migGMI64LOT81"
  }
}
