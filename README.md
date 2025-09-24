# Category Migration Lambda

A professional AWS Lambda function following BuiltIn.Net.Templates standards to migrate category and subcategory IDs in job preferences from legacy IDs to new IDs.

## 🏗️ Project Structure

```
CategoryMigrationLambda/
├── CategoryMigrationLambda/           # Main Lambda project
│   ├── Data/                         # Data models and DTOs
│   ├── Services/                     # Business logic services
│   ├── Extensions/                   # Service collection extensions
│   ├── Function.cs                   # Lambda entry point
│   └── *.csproj                     # Project file
├── CategoryMigrationLambda.Tests/    # Unit tests
├── scripts/                          # Deployment scripts
├── CategoryMigrationLambda.sln       # Solution file
└── README.md                         # This file
```

## 🚀 Quick Start

### Deploy
```bash
cd scripts
chmod +x deploy-lambda.sh
./deploy-lambda.sh
```

### Run Migration
```bash
# Migrate all preferences (dry run)
aws lambda invoke \
  --function-name category-migration-lambda \
  --payload '{"type":"all","dryRun":true}' \
  response.json

# Migrate all preferences (actual)
aws lambda invoke \
  --function-name category-migration-lambda \
  --payload '{"type":"all","dryRun":false}' \
  response.json

# Migrate specific user
aws lambda invoke \
  --function-name category-migration-lambda \
  --payload '{"type":"user","subjectId":"user-123","dryRun":true}' \
  response.json
```

### Monitor
```bash
aws logs tail /aws/lambda/category-migration-lambda --follow
```

## 🧪 Development

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Run Locally
```bash
dotnet lambda-test-tool
```

## 📋 What It Does

- **Scans** DynamoDB Users table for job preferences with legacy category IDs (1000+)
- **Maps** legacy IDs to new IDs using embedded category mappings
- **Updates** preferences in batches of 25 for efficiency
- **Logs** all operations and errors to CloudWatch
- **Supports** dry run mode for safe testing

## 🔧 Configuration

- **Runtime**: .NET 8
- **Memory**: 1024 MB  
- **Timeout**: 15 minutes
- **Table**: Users (DynamoDB)
- **Batch Size**: 25 items per batch

## 📊 Example Mappings

| Legacy Category | Legacy Subcategory | New Category | New Subcategory |
|----------------|-------------------|--------------|-----------------|
| 1001 (Consulting) | null | 1 (Consulting) | null |
| 147 (Data + Analytics) | 508 (Analytics) | 4 (Data & Analytics) | 38 (Reporting & Insights) |
| 151 (Internships) | null | null | null |

## ⚠️ Important

- **Backup** your DynamoDB table before running
- **Test** with dry run first: `{"type":"all","dryRun":true}`
- **Monitor** CloudWatch logs during migration
- **Start** with specific user: `{"type":"user","subjectId":"test-user"}`

## 🏛️ Architecture

- **Dependency Injection**: Proper service registration and configuration
- **Configuration**: Environment-based settings with appsettings.json
- **Logging**: Structured logging with CloudWatch integration
- **Error Handling**: Comprehensive error tracking and reporting
- **Testing**: Unit tests with proper mocking and test utilities

## 📁 Standalone Project

This Lambda project is completely standalone and doesn't depend on the main usersapi project. This prevents IDE issues with nested solutions and makes it easier to manage and deploy independently.
