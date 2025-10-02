terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "5.38.0"
    }
  }
}

provider "aws" {
  default_tags {
    tags = {
      owning_team  = local.owning_team
      product      = local.product
      map-migrated = "migGMI64LOT81" # uturn
    }
  }
  region = "us-west-2"
}
