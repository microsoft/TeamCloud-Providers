#!/bin/bash

trace() {
    echo -e "\n>>> $@ ...\n"
}

trace "Terraform plans" 
find . -name "*.tf" -exec echo -e "\n>$@\n" \; -exec cat {} \;

trace "Initializing Terraform"
terraform init -no-color

trace "Applying Terraform Plan"
terraform apply -no-color -auto-approve -var "ComponentResourceGroupName=$ComponentResourceGroup"

# tail -f /dev/null
