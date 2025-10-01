# Category Migration Lambda Terraform Configuration

This directory contains the Terraform configuration for the Category Migration Lambda infrastructure, designed to be scalable and follow enterprise patterns.

## Structure

```
workspace/
├── lambdas.tf                    # Main lambda functions configuration
├── modules/
│   └── lambda/                   # Reusable lambda module
│       ├── main.tf              # Lambda module implementation
│       ├── variables.tf         # Module variables
│       └── outputs.tf           # Module outputs
├── variables.tf                  # Global variables
├── envs.auto.tfvars             # Environment-specific variables
├── provider.tf                  # AWS provider configuration
├── locals.tf                    # Local values
├── cloudwatch.tf                # CloudWatch monitoring
└── outputs.tf                   # Outputs
```

## Lambda Functions

### Current Implementation
- **Category Migration Lambda**: `category-migration-lambda`
  - Handler: `CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler`
  - Runtime: `dotnet8`
  - Scheduled execution via CloudWatch Events
  - Environment-specific configurations

## Module Features

The lambda module provides:
- ✅ Lambda function with configurable runtime, timeout, and memory
- ✅ IAM role with basic and VPC execution policies
- ✅ CloudWatch log group with configurable retention
- ✅ Security group for VPC access (if configured)
- ✅ CloudWatch Events scheduling (optional)
- ✅ Proper tagging and naming conventions
- ✅ Environment variable support
- ✅ DynamoDB access policies
- ✅ CloudWatch metrics policies

## Environment Variables

Each environment has different configurations:

- **develop**: Frequent execution (5 minutes), Information logging
- **staging**: Moderate execution (15 minutes), Information logging
- **prod**: Hourly execution, Warning logging

## Deployment

The infrastructure is deployed via CircleCI with the following workflow:
1. Run tests
2. Plan terraform changes
3. Package lambda functions
4. Deploy to develop
5. Approval for staging
6. Deploy to staging
7. Approval for production
8. Deploy to production

## Monitoring

- CloudWatch Dashboard for metrics visualization
- CloudWatch Alarms for error and duration monitoring
- Configurable log retention by environment
- Custom metrics support

## Usage

### Initialize Terraform
```bash
cd terraform/workspace
terraform init
```

### Plan Changes
```bash
terraform plan -var="environment=develop"
```

### Apply Changes
```bash
terraform apply -var="environment=develop"
```

### Switch Environments
```bash
terraform workspace select develop
terraform workspace select staging
terraform workspace select prod
```
