#!/bin/bash

trace() {
    echo -e "\n>>> $@ ...\n"
}

readonly ComponentStateFile="/mnt/storage/terraform.tfstate"

ComponentTemplateFile="$(echo "$ComponentTemplateFolder/main.tf" | sed 's/^file:\/\///g')"
ComponentTemplatePlan="$(echo "$ComponentTemplateFile.plan")"
ComponentTemplateJson="$(cat $ComponentTemplateFile | hcl2json)"

# echo "$ComponentTemplateJson"

trace "Terraform Info"
terraform -version

trace "Initializing Terraform"
terraform init -no-color

trace "Updating Terraform Plan"
terraform plan -no-color -refresh=true -lock=true -destroy -state=$ComponentStateFile -out=$ComponentTemplatePlan -var "resourceGroupName=$ComponentResourceGroup" -var "resourceGroupLocation=$(az group show -n $ComponentResourceGroup --query location -o tsv)"

trace "Applying Terraform Plan"
terraform apply -no-color -auto-approve -lock=true -state=$ComponentStateFile $ComponentTemplatePlan

# tail -f /dev/null
