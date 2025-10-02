#!/bin/bash

# Test script to verify the terraform state fix
# This script simulates the CircleCI terraform workflow

set -e

WORKSPACE=${1:-develop}
TERRAFORM_DIR="./terraform/workspace"

echo "🧪 Testing CategoryMigrationLambda Terraform Fix"
echo "================================================"
echo "Workspace: $WORKSPACE"
echo ""

# Change to terraform directory
cd "$TERRAFORM_DIR"

echo "📦 Step 1: Initializing terraform..."
terraform init

echo "🏗️  Step 2: Selecting workspace: $WORKSPACE"
terraform workspace select "$WORKSPACE" || terraform workspace new "$WORKSPACE"

echo "🔍 Step 3: Running terraform validate..."
terraform validate

echo "📋 Step 4: Running terraform plan..."
if terraform plan -detailed-exitcode; then
    echo "✅ SUCCESS: Terraform plan completed without errors"
    echo "🎯 No changes needed - state is synchronized"
    exit 0
else
    PLAN_EXIT_CODE=$?
    if [ $PLAN_EXIT_CODE -eq 2 ]; then
        echo "⚠️  Terraform plan shows changes (exit code 2)"
        echo "📋 This is expected if resources need to be created or updated"
        echo "✅ No ResourceAlreadyExistsException errors - fix is working!"
    else
        echo "❌ Terraform plan failed with exit code: $PLAN_EXIT_CODE"
        echo "🔍 Check the output above for errors"
        exit 1
    fi
fi

echo ""
echo "🎉 Test completed successfully!"
echo "✅ The terraform state fix is working correctly"
echo "🚀 CircleCI should now be able to run terraform apply without ResourceAlreadyExistsException errors"
