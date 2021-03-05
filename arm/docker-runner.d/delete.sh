#!/bin/bash
DIR=$(dirname "$0")
. $DIR/_common.sh

deleteResourceGroup() {

    local ComponentResourceGroup="$1" 
    local EnvironmentDeploymentName="$(uuidgen)"
    
    echo -e "Deleting resource group: $ComponentResourceGroup"

    DeploymentOutput=$(az deployment group create   --resource-group "$ComponentResourceGroup" \
                                                    --name "$EnvironmentDeploymentName" \
                                                    --no-prompt true --no-wait --mode Complete \
                                                    --template-file "$DIR/empty.json" 2>&1)

    if [ $? -eq 0 ]; then # deployment successfully created

        while true; do

            sleep 1

            ProvisioningState=$(az deployment group show --resource-group "$ComponentResourceGroup" --name "$EnvironmentDeploymentName" --query "properties.provisioningState" -o tsv)
            ProvisioningDetails=$(az deployment operation group list --resource-group "$ComponentResourceGroup" --name "$EnvironmentDeploymentName")

            trackDeployment "$ProvisioningDetails"
            
            if [[ "CANCELED|FAILED|SUCCEEDED" == *"${ProvisioningState^^}"* ]]; then

                echo "Deployment $EnvironmentDeploymentName: $ProvisioningState"
                break
            fi

        done
    fi

    if [ ! -z "$DeploymentOutput" ]; then

        if [ $(echo "$DeploymentOutput" | jq empty > /dev/null 2>&1; echo $?) -eq 0 ]; then

            DeploymentOutput="$( echo $DeploymentOutput | jq --raw-output '.[] | .details[] | "Error: \(.message)\n"' | sed 's/\\n/\n/g'  )"

        fi

        echo "$DeploymentOutput" && exit 1 # our script failed to enqueue a new deployment - we return a none zero exit code to inidicate this

    fi
}

if [ -z "$ComponentResourceGroup" ]; then

    $(az group list --query "[].name" -o tsv) | while read rg; do
        deleteResourceGroup "$rg"
    done

else

    deleteResourceGroup "$ComponentResourceGroup"

fi
