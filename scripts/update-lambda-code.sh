#!/bin/bash

# Update Lambda function code with actual .NET package
# This script should be run after Terraform creates the Lambda function

set -e

# Configuration
FUNCTION_NAME="category-migration-lambda-${WORKSPACE:-dev}"
REGION="us-west-2"
PACKAGE_NAME="category-migration-lambda.zip"

echo "🚀 Updating Lambda function code for Category Migration..."

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
cd CategoryMigrationLambda
dotnet lambda package --configuration Release --framework net8.0 --output-package ../$PACKAGE_NAME

# Get the package file
PACKAGE_FILE="../$PACKAGE_NAME"
if [ ! -f "$PACKAGE_FILE" ]; then
    echo "❌ Failed to create Lambda package"
    exit 1
fi

echo "✅ Lambda package created: $PACKAGE_FILE"

# Update Lambda function code
echo "🚀 Updating Lambda function code..."
aws lambda update-function-code \
    --function-name $FUNCTION_NAME \
    --zip-file fileb://$PACKAGE_FILE \
    --region $REGION

echo "✅ Lambda function code updated successfully!"

# Clean up
rm -f $PACKAGE_FILE

echo "🎉 Deployment completed!"
echo ""
echo "📋 Next steps:"
echo "1. Test the function: aws lambda invoke --function-name $FUNCTION_NAME --payload '{\"type\":\"all\",\"dryRun\":true}' response.json"
echo "2. Monitor logs: aws logs tail /aws/lambda/$FUNCTION_NAME --follow"
echo ""
echo "🔧 Function details:"
echo "   Name: $FUNCTION_NAME"
echo "   Runtime: dotnet8"
echo "   Handler: CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler"
