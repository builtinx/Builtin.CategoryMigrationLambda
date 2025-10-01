# Category Migration Lambda Infrastructure
# Based on llm-api-gateway patterns

terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Configure the AWS Provider
provider "aws" {
  region = var.aws_region
  
  default_tags {
    tags = {
      Project     = "CategoryMigrationLambda"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

# Data sources
data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

# Local values
locals {
  function_name = "category-migration-lambda"
  role_name     = "CategoryMigrationLambdaRole"
  
  common_tags = {
    Project     = "CategoryMigrationLambda"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}
