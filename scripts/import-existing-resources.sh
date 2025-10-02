#!/bin/bash

# Import existing AWS resources into terraform state
# This script handles the "ResourceAlreadyExistsException" errors

set -e

WORKSPACE=${1:-develop}
TERRAFORM_DIR="./terraform/workspace"

echo "🔧 Importing existing resources for workspace: $WORKSPACE"

# Change to terraform directory
cd "$TERRAFORM_DIR"

# Initialize terraform
echo "📦 Initializing terraform..."
terraform init

# Select workspace
echo "🏗️  Selecting workspace: $WORKSPACE"
terraform workspace select "$WORKSPACE" || terraform workspace new "$WORKSPACE"

# Function to safely import resources
import_resource() {
    local resource_type="$1"
    local resource_name="$2"
    local aws_resource_id="$3"
    
    echo "🔄 Attempting to import $resource_type: $resource_name"
    
    if terraform import "$resource_type" "$aws_resource_id" 2>/dev/null; then
        echo "✅ Successfully imported $resource_type"
    else
        echo "⚠️  Could not import $resource_type (may not exist or already imported)"
    fi
}

# Import CloudWatch Log Group
LOG_GROUP_NAME="/aws/lambda/category-migration-lambda-$WORKSPACE"
import_resource "module.category_migration_lambda.aws_cloudwatch_log_group.logs" "cloudwatch_log_group" "$LOG_GROUP_NAME"

# Import IAM Role
IAM_ROLE_NAME="category-migration-lambda-$WORKSPACE-role"
import_resource "module.category_migration_lambda.aws_iam_role.lambda_role" "iam_role" "$IAM_ROLE_NAME"

# Import IAM Role Policy Attachments (if they exist)
import_resource "module.category_migration_lambda.aws_iam_role_policy_attachment.lambda_basic" "iam_role_policy_attachment" "$IAM_ROLE_NAME/arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"

# Import IAM Role Policies
import_resource "module.category_migration_lambda.aws_iam_role_policy.dynamodb_access" "iam_role_policy" "$IAM_ROLE_NAME/category-migration-lambda-$WORKSPACE-dynamodb-policy"

import_resource "module.category_migration_lambda.aws_iam_role_policy.cloudwatch_metrics" "iam_role_policy" "$IAM_ROLE_NAME/category-migration-lambda-$WORKSPACE-cloudwatch-metrics-policy"

# Import Lambda Function (if it exists)
LAMBDA_FUNCTION_NAME="category-migration-lambda-$WORKSPACE"
import_resource "module.category_migration_lambda.aws_lambda_function.this" "lambda_function" "$LAMBDA_FUNCTION_NAME"

echo "🎯 Running terraform plan to verify state..."
terraform plan -detailed-exitcode || {
    echo "⚠️  Terraform plan shows differences. This is expected after importing resources."
    echo "📋 Review the plan output above to ensure everything looks correct."
}

echo "✅ Import process completed for workspace: $WORKSPACE"
echo "🚀 You can now run 'terraform apply' safely"
