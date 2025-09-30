# Category Migration Lambda - Terraform Infrastructure

This directory contains Terraform configuration for deploying the Category Migration Lambda infrastructure to AWS.

## Prerequisites

- Terraform >= 1.0
- AWS CLI configured with appropriate credentials
- AWS account with necessary permissions

## Quick Start

1. **Copy the example variables file:**
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```

2. **Customize the variables in `terraform.tfvars`:**
   ```hcl
   aws_region = "us-west-2"
   environment = "dev"
   # ... other variables
   ```

3. **Initialize Terraform:**
   ```bash
   cd terraform
   terraform init
   ```

4. **Plan the deployment:**
   ```bash
   terraform plan
   ```

5. **Apply the configuration:**
   ```bash
   terraform apply
   ```

## Infrastructure Components

### Lambda Function
- **Function Name**: `category-migration-lambda`
- **Runtime**: .NET 8
- **Memory**: 1024 MB
- **Timeout**: 15 minutes
- **Handler**: `CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler`

### IAM Role and Policies
- **Execution Role**: Basic Lambda execution permissions
- **DynamoDB Access**: Full access to the Users table
- **X-Ray Tracing**: Optional tracing for debugging

### CloudWatch Monitoring
- **Log Group**: `/aws/lambda/category-migration-lambda`
- **Alarms**: Errors, Duration, Throttles
- **Dashboard**: Visual monitoring of key metrics
- **SNS Alerts**: Production environment alerts

## Environment-Specific Configuration

### Development
- Minimal monitoring
- Shorter log retention
- No SNS alerts

### Production
- Full monitoring suite
- Extended log retention
- SNS alerts for critical issues
- Reserved concurrency limits

## Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `aws_region` | AWS region for resources | `us-west-2` | No |
| `environment` | Environment name (dev/staging/production) | `dev` | No |
| `function_name` | Lambda function name | `category-migration-lambda` | No |
| `runtime` | Lambda runtime | `dotnet8` | No |
| `handler` | Lambda handler | `CategoryMigrationLambda::CategoryMigrationLambda.Function::FunctionHandler` | No |
| `timeout` | Lambda timeout in seconds | `900` | No |
| `memory_size` | Lambda memory size in MB | `1024` | No |
| `dynamodb_table_name` | DynamoDB table name | `Users` | No |
| `log_retention_days` | CloudWatch log retention | `14` | No |
| `enable_xray_tracing` | Enable X-Ray tracing | `true` | No |
| `reserved_concurrency` | Reserved concurrency | `1` | No |

## Outputs

After deployment, Terraform provides these outputs:

- `lambda_function_name`: Name of the deployed Lambda function
- `lambda_function_arn`: ARN of the Lambda function
- `lambda_function_invoke_arn`: Invoke ARN for API Gateway integration
- `lambda_role_arn`: ARN of the execution role
- `cloudwatch_log_group_name`: Name of the log group
- `cloudwatch_dashboard_url`: URL to the monitoring dashboard
- `invoke_lambda_example`: Example command to test the function
- `view_logs_example`: Example command to view logs

## Usage Examples

### Test the Lambda Function
```bash
aws lambda invoke \
  --function-name category-migration-lambda \
  --payload '{"type":"all","dryRun":true}' \
  --cli-binary-format raw-in-base64-out \
  response.json
```

### View Logs
```bash
aws logs tail /aws/lambda/category-migration-lambda --follow
```

### Monitor with CloudWatch Dashboard
Visit the dashboard URL provided in the Terraform outputs.

## CI/CD Integration

This Terraform configuration is designed to work with the CircleCI configuration in `.circleci/config.yml`:

1. **Feature branches**: Deploy Lambda function only
2. **Main branch**: Deploy infrastructure + Lambda function + run tests

## Security Considerations

- IAM roles follow the principle of least privilege
- DynamoDB access is scoped to the specific table
- X-Ray tracing can be disabled for sensitive environments
- SNS alerts are only created in production

## Cost Optimization

- Log retention is configurable (default: 14 days)
- Reserved concurrency prevents runaway costs
- X-Ray tracing can be disabled to reduce costs
- Environment-specific resource sizing

## Troubleshooting

### Common Issues

1. **Permission Denied**: Ensure AWS credentials have sufficient permissions
2. **DynamoDB Access**: Verify the table name exists and is accessible
3. **Lambda Timeout**: Adjust timeout value for long-running migrations
4. **Memory Issues**: Increase memory size for large datasets

### Debugging

1. Check CloudWatch logs for detailed error messages
2. Use X-Ray tracing to identify performance bottlenecks
3. Monitor CloudWatch alarms for operational issues
4. Review IAM policies for permission problems

## Cleanup

To destroy the infrastructure:

```bash
terraform destroy
```

**Warning**: This will delete all resources including CloudWatch logs and monitoring data.
