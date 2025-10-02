terraform {
  backend "s3" {
    bucket         = "us-west-2-builtin-terraform-remote-state"
    dynamodb_table = "terraform-remote-state"
    encrypt        = true
    key            = "category-migration-lambda.tfstate"
    region         = "us-west-2"
    assume_role = {
      role_arn = "arn:aws:iam::489100804770:role/terraform"
    }
    workspace_key_prefix = "category-migration-lambda"
  }
}
