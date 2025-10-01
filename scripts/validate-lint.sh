#!/bin/bash

# Validation and linting script for CategoryMigrationLambda
# This script validates the project structure and runs linting

set -e

echo "🔍 Validating CategoryMigrationLambda project..."

# Check if we're in the right directory
if [ ! -f "CategoryMigrationLambda.sln" ]; then
    echo "❌ Error: Not in the CategoryMigrationLambda project root"
    exit 1
fi

# Validate .NET project
echo "📦 Validating .NET project..."
dotnet build --configuration Release --no-restore
echo "✅ .NET build successful"

# Run tests
echo "🧪 Running tests..."
dotnet test --configuration Release --no-build --verbosity normal
echo "✅ Tests passed"

# Validate Terraform
echo "🏗️ Validating Terraform configuration..."
cd terraform/workspace
terraform fmt -check
terraform validate
echo "✅ Terraform validation successful"

# Check CircleCI config
echo "🔄 Validating CircleCI configuration..."
cd ../..
if command -v circleci &> /dev/null; then
    circleci config validate .circleci/config.yml
    echo "✅ CircleCI configuration valid"
else
    echo "⚠️ CircleCI CLI not found, skipping validation"
fi

echo "🎉 All validations passed!"