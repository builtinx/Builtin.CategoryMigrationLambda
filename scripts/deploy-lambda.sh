#!/bin/bash

# AWS Lambda deployment script for Category Migration
# This script packages and deploys the Lambda function using the proper project structure

set -e

# Configuration
FUNCTION_NAME="category-migration-lambda"
RUNTIME="dotnet8"
HANDLER="CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler"
ROLE_NAME="CategoryMigrationLambdaRole"
REGION="us-west-2"
PACKAGE_NAME="category-migration-lambda.zip"

echo "🚀 Starting AWS Lambda deployment for Category Migration..."

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo "❌ AWS CLI is not installed. Please install it first."
    exit 1
fi

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET CLI is not installed. Please install it first."
    exit 1
fi

# Navigate to the project root
cd "$(dirname "$0")/.."

# Build the project
echo "🔨 Building Lambda project..."
dotnet build --configuration Release

# Package the Lambda function
echo "📦 Packaging Lambda function..."
cd src/CategoryMigrationLambda
dotnet lambda package --configuration Release --framework net8.0

# Get the package file
PACKAGE_FILE=$(find . -name "*.zip" -type f | head -1)
if [ -z "$PACKAGE_FILE" ]; then
    echo "❌ Failed to create Lambda package"
    exit 1
fi

echo "✅ Lambda package created: $PACKAGE_FILE"

# Create IAM role if it doesn't exist
echo "🔐 Creating IAM role..."
ROLE_ARN=$(aws iam get-role --role-name $ROLE_NAME --query 'Role.Arn' --output text 2>/dev/null || echo "")

if [ -z "$ROLE_ARN" ]; then
    echo "Creating IAM role: $ROLE_NAME"
    
    # Create trust policy
    cat > trust-policy.json << EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Principal": {
                "Service": "lambda.amazonaws.com"
            },
            "Action": "sts:AssumeRole"
        }
    ]
}
EOF

    # Create role
    aws iam create-role \
        --role-name $ROLE_NAME \
        --assume-role-policy-document file://trust-policy.json

    # Attach basic execution policy
    aws iam attach-role-policy \
        --role-name $ROLE_NAME \
        --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole

    # Attach DynamoDB policy
    cat > dynamodb-policy.json << EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "dynamodb:GetItem",
                "dynamodb:PutItem",
                "dynamodb:UpdateItem",
                "dynamodb:DeleteItem",
                "dynamodb:Query",
                "dynamodb:Scan",
                "dynamodb:BatchGetItem",
                "dynamodb:BatchWriteItem"
            ],
            "Resource": "arn:aws:dynamodb:$REGION:*:table/Users"
        }
    ]
}
EOF

    aws iam put-role-policy \
        --role-name $ROLE_NAME \
        --policy-name DynamoDBAccess \
        --policy-document file://dynamodb-policy.json

    # Wait for role to be ready
    echo "⏳ Waiting for IAM role to be ready..."
    sleep 10
    
    ROLE_ARN=$(aws iam get-role --role-name $ROLE_NAME --query 'Role.Arn' --output text)
fi

echo "✅ IAM role ready: $ROLE_ARN"

# Deploy or update Lambda function
echo "🚀 Deploying Lambda function..."
FUNCTION_EXISTS=$(aws lambda get-function --function-name $FUNCTION_NAME --query 'Configuration.FunctionName' --output text 2>/dev/null || echo "")

if [ -z "$FUNCTION_EXISTS" ]; then
    echo "Creating new Lambda function..."
    aws lambda create-function \
        --function-name $FUNCTION_NAME \
        --runtime $RUNTIME \
        --role $ROLE_ARN \
        --handler $HANDLER \
        --zip-file fileb://$PACKAGE_FILE \
        --timeout 900 \
        --memory-size 1024 \
        --description "Category and subcategory migration for job preferences"
else
    echo "Updating existing Lambda function..."
    aws lambda update-function-code \
        --function-name $FUNCTION_NAME \
        --zip-file fileb://$PACKAGE_FILE
    
    aws lambda update-function-configuration \
        --function-name $FUNCTION_NAME \
        --timeout 900 \
        --memory-size 1024
fi

echo "✅ Lambda function deployed successfully!"

# Clean up
rm -f trust-policy.json dynamodb-policy.json

echo "🎉 Deployment completed!"
echo ""
echo "📋 Next steps:"
echo "1. Test the function: aws lambda invoke --function-name $FUNCTION_NAME --payload '{\"type\":\"all\",\"dryRun\":true}' response.json"
echo "2. Monitor logs: aws logs tail /aws/lambda/$FUNCTION_NAME --follow"
echo "3. Set up CloudWatch alarms for monitoring"
echo ""
echo "🔧 Function details:"
echo "   Name: $FUNCTION_NAME"
echo "   Runtime: $RUNTIME"
echo "   Handler: $HANDLER"
echo "   Timeout: 15 minutes"
echo "   Memory: 1024 MB"
echo ""
echo "💡 This version follows BuiltIn.Net.Templates standards with proper project structure"
echo "   and includes all category mappings embedded in the code."