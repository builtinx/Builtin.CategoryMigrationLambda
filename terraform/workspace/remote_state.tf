data "terraform_remote_state" "infrastructure" {
  backend = "remote"
  config = {
    organization = "builtin"
    workspaces = {
      name = "infrastructure-${local.workspace}"
    }
  }
}
