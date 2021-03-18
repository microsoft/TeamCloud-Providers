#!/bin/bash

trace() {
    echo -e "\n>>> $@ ...\n"
}

readonly ComponentState="/mnt/storage/component.tfstate"
readonly ComponentPlan="/mnt/storage/component.tfplan"

rm -f $ComponentPlan # reset terraform plan to start fresh

trace "Terraform Info"
terraform -version

trace "Initializing Terraform"
terraform init -no-color

trace "Creating Terraform Plan"
terraform plan -no-color -refresh=true -lock=true -state=$ComponentState -out=$ComponentPlan -var "resourceGroupName=$ComponentResourceGroup"

trace "Applying Terraform Plan"
terraform apply -no-color -auto-approve -lock=true -state=$ComponentState $ComponentPlan

# tail -f /dev/null
