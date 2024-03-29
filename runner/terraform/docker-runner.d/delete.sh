#!/bin/bash

DIR=$(dirname "$0")
. $DIR/_common.sh

trace() {
    echo -e "\n>>> $@ ...\n"
}

readonly ComponentState="/mnt/storage/component.tfstate"
readonly ComponentPlan="/mnt/temporary/component.tfplan"
readonly ComponentVars="/mnt/temporary/component.tfvars.json"

echo "$ComponentTemplateParameters" > $ComponentVars

trace "Terraform Info"
terraform -version

trace "Initializing Terraform"
terraform init -no-color

trace "Creating Terraform Plan"
terraform plan -no-color -compact-warnings -destroy -refresh=true -lock=true -state=$ComponentState -out=$ComponentPlan -var-file="$ComponentVars" -var "resourceGroupName=$ComponentResourceGroup"

trace "Applying Terraform Plan"
terraform apply -no-color -compact-warnings -auto-approve -lock=true -state=$ComponentState $ComponentPlan

updateComponentValue

# tail -f /dev/null
