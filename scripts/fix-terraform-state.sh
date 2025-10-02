#!/bin/bash

# Manual fix for CategoryMigrationLambda terraform state issues
# This script handles the "ResourceAlreadyExistsException" errors
# NOTE: CircleCI now handles this automatically, this script is for manual use only

set -e

WORKSPACE=${1:-develop}
TERRAFORM_DIR="./terraform/workspace"

echo "🚀 CategoryMigrationLambda Terraform State Fix (Manual)"
echo "======================================================"
echo "Workspace: $WORKSPACE"
echo "Note: CircleCI now handles resource import automatically"
echo ""

# Change to terraform directory
cd "$TERRAFORM_DIR"

echo "📦 Initializing terraform..."
terraform init

echo "🏗️  Selecting workspace: $WORKSPACE"
terraform workspace select "$WORKSPACE" || terraform workspace new "$WORKSPACE"

echo ""
echo "🔍 Checking for existing resources..."

# Check if resources exist in AWS
LOG_GROUP_NAME="/aws/lambda/category-migration-lambda-$WORKSPACE"
IAM_ROLE_NAME="category-migration-lambda-$WORKSPACE-role"
LAMBDA_FUNCTION_NAME="category-migration-lambda-$WORKSPACE"

echo "Checking CloudWatch Log Group: $LOG_GROUP_NAME"
if aws logs describe-log-groups --log-group-name-prefix "$LOG_GROUP_NAME" --query 'logGroups[?logGroupName==`'$LOG_GROUP_NAME'`]' --output text | grep -q "$LOG_GROUP_NAME"; then
    echo "✅ Log group exists: $LOG_GROUP_NAME"
    LOG_GROUP_EXISTS=true
else
    echo "❌ Log group does not exist: $LOG_GROUP_NAME"
    LOG_GROUP_EXISTS=false
fi

echo "Checking IAM Role: $IAM_ROLE_NAME"
if aws iam get-role --role-name "$IAM_ROLE_NAME" >/dev/null 2>&1; then
    echo "✅ IAM role exists: $IAM_ROLE_NAME"
    IAM_ROLE_EXISTS=true
else
    echo "❌ IAM role does not exist: $IAM_ROLE_NAME"
    IAM_ROLE_EXISTS=false
fi

echo "Checking Lambda Function: $LAMBDA_FUNCTION_NAME"
if aws lambda get-function --function-name "$LAMBDA_FUNCTION_NAME" >/dev/null 2>&1; then
    echo "✅ Lambda function exists: $LAMBDA_FUNCTION_NAME"
    LAMBDA_EXISTS=true
else
    echo "❌ Lambda function does not exist: $LAMBDA_FUNCTION_NAME"
    LAMBDA_EXISTS=false
fi

echo ""
echo "🔄 Importing existing resources into terraform state..."

# Import CloudWatch Log Group
if [ "$LOG_GROUP_EXISTS" = true ]; then
    echo "Importing CloudWatch Log Group..."
    if terraform import "module.category_migration_lambda.aws_cloudwatch_log_group.logs" "$LOG_GROUP_NAME" 2>/dev/null; then
        echo "✅ Successfully imported CloudWatch Log Group"
    else
        echo "⚠️  CloudWatch Log Group may already be in state"
    fi
fi

# Import IAM Role
if [ "$IAM_ROLE_EXISTS" = true ]; then
    echo "Importing IAM Role..."
    if terraform import "module.category_migration_lambda.aws_iam_role.lambda_role" "$IAM_ROLE_NAME" 2>/dev/null; then
        echo "✅ Successfully imported IAM Role"
    else
        echo "⚠️  IAM Role may already be in state"
    fi
    
    # Import IAM Role Policy Attachments
    echo "Importing IAM Role Policy Attachments..."
    terraform import "module.category_migration_lambda.aws_iam_role_policy_attachment.lambda_basic" "$IAM_ROLE_NAME/arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole" 2>/dev/null || echo "⚠️  Basic execution policy attachment may already be in state"
    
    # Import IAM Role Policies
    echo "Importing IAM Role Policies..."
    terraform import "module.category_migration_lambda.aws_iam_role_policy.dynamodb_access" "$IAM_ROLE_NAME/category-migration-lambda-$WORKSPACE-dynamodb-policy" 2>/dev/null || echo "⚠️  DynamoDB policy may already be in state"
    terraform import "module.category_migration_lambda.aws_iam_role_policy.cloudwatch_metrics" "$IAM_ROLE_NAME/category-migration-lambda-$WORKSPACE-cloudwatch-metrics-policy" 2>/dev/null || echo "⚠️  CloudWatch metrics policy may already be in state"
fi

# Import Lambda Function
if [ "$LAMBDA_EXISTS" = true ]; then
    echo "Importing Lambda Function..."
    if terraform import "module.category_migration_lambda.aws_lambda_function.this" "$LAMBDA_FUNCTION_NAME" 2>/dev/null; then
        echo "✅ Successfully imported Lambda Function"
    else
        echo "⚠️  Lambda Function may already be in state"
    fi
fi

echo ""
echo "🎯 Running terraform plan to verify state..."
echo "=============================================="

if terraform plan -detailed-exitcode; then
    echo ""
    echo "✅ SUCCESS! Terraform state is now synchronized"
    echo "🚀 You can now run 'terraform apply' safely"
    echo ""
    echo "Next steps:"
    echo "1. Run 'terraform apply' to deploy any missing resources"
    echo "2. Your CircleCI pipeline should now work without ResourceAlreadyExistsException errors"
else
    echo ""
    echo "⚠️  Terraform plan shows differences. This is expected after importing resources."
    echo "📋 Review the plan output above to ensure everything looks correct."
    echo ""
    echo "If the plan looks good, you can run 'terraform apply' to complete the deployment."
fi

echo ""
echo "🔧 Fix completed for workspace: $WORKSPACE"
