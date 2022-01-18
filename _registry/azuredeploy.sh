#!/bin/bash
csd=$(dirname "$0")

TARGET_SUBSCRIPTION="$1"
TARGET_RESOURCEGROUP="$2"
TARGET_RESOURCEGROUPLOCATION="$3"

# select the target subscription
echo "Selecting subscription $TARGET_SUBSCRIPTION ..."
az account set --subscription $TARGET_SUBSCRIPTION

# prepare resource group
if [ "$(az group exists -g $TARGET_RESOURCEGROUP)" == "false" ]; then
	echo "Creating resource group $TARGET_RESOURCEGROUP ..."
	az group create -n $TARGET_RESOURCEGROUP -l $TARGET_RESOURCEGROUPLOCATION
fi

# deploy resources to resource group
echo "Deploying resources ..."
az deployment group create -g $TARGET_RESOURCEGROUP -n $(uuidgen) --mode Incremental --template-file $csd/azuredeploy.json --parameter registryLocations="['eastus', 'westus', 'westeurope']"