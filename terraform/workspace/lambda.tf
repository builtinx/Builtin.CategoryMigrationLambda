# Terraform requires code to deploy a Lambda.  This creates a placeholder that can be replaced by CircleCI later to resolve a circular dependency.
data "archive_file" "placeholder-code" {
  type        = "zip"
  output_path = "placeholder.zip"

  source {
    content  = "Placeholder"
    filename = "placeholder.txt"
  }
}

data "aws_subnets" "this" {
  filter {
    name   = "tag:Name"
    values = [for location in ["us-west-2a", "us-west-2b", "us-west-2c", "us-west-2d"] : "lambda_private_subnet-${location}"]
  }
  filter {
    name   = "vpc-id"
    values = [data.terraform_remote_state.infrastructure.outputs.main_vpc_id]
  }
} 