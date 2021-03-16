#!/bin/bash

trace() {
    echo -e "\n>>> $@ ...\n"
}

readonly ComponentStateFile="/mnt/storage/terraform.tfstate"

ComponentTemplateFile="$(echo "$ComponentTemplateFolder/azuredeploy.tf" | sed 's/^file:\/\///g')"
ComponentTemplateJson="$(cat $ComponentTemplateFile | hcl2json)"

echo "$ComponentTemplateJson"

trace "Initializing Terraform"
terraform init -no-color

trace "Destroying Terraform Plan"
terraform destroy -no-color -auto-approve -state=$ComponentStateFile

# tail -f /dev/null
