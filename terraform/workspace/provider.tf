terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "5.38.0"
    }
    vault = {
      source  = "hashicorp/vault"
      version = "5.1.0"
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

provider "vault" {
  address = var.vault_addr[local.workspace]
}
