#!/bin/bash

DIR=$(dirname "$0")
. $DIR/_common.sh

trace() {
    echo -e "\n>>> $@ ...\n"
}

ComponentDeploymentName="$(uuidgen)"
ComponentDeploymentOutput=""

if [ ! -z "$(find . -name '*.bicep' -print -quit)" ] ; then
    trace "Transpiling BICEP template" 
    find . -name "*.bicep" -exec echo "- {}" \; -exec az bicep build --files {} \;
fi

ComponentTemplateFile="$(echo "$ComponentTemplateFolder/azuredeploy.json" | sed 's/^file:\/\///g')"
ComponentTemplateUrl="$(echo "$ComponentTemplateBaseUrl/azuredeploy.json" | sed 's/^http:/https:/g')"
ComponentTemplateParametersJson=$(echo "$ComponentTemplateParameters" | jq --compact-output '{ "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#", "contentVersion": "1.0.0.0", "parameters": (to_entries | if length == 0 then {} else (map( { (.key): { "value": .value } } ) | add) end) }' )

while read p; do
    case "$p" in
        _artifactsLocation)
            ComponentTemplateParametersOpts+=( --parameters _artifactsLocation="$(dirname $ComponentTemplateUrl)" )
            ;;
        _artifactsLocationSasToken)
            ComponentTemplateParametersOpts+=( --parameters _artifactsLocationSasToken="?code=$TaskToken" )
            ;;
    esac
done < <( echo "$( cat "$ComponentTemplateFile" | jq --raw-output '.parameters | to_entries[] | select( .key | startswith("_artifactsLocation")) | .key' )" )

trace "Deploying ARM template"

if [ -z "$ComponentResourceGroup" ]; then

    if [ "$WebServerEnabled" == "1" ]; then

        ComponentDeploymentOutput=$(az deployment sub create --subscription $ComponentSubscription \
                                                    --location "$ComponentLocation" \
                                                    --name "$ComponentDeploymentName" \
                                                    --no-prompt true --no-wait \
                                                    --template-uri "$ComponentTemplateUrl" \
                                                    --query-string "code=$TaskToken" \
                                                    --parameters "$ComponentTemplateParametersJson" \
                                                    "${ComponentTemplateParametersOpts[@]}" 2>&1)
    
    else

        ComponentDeploymentOutput=$(az deployment sub create --subscription $ComponentSubscription \
                                                    --location "$ComponentLocation" \
                                                    --name "$ComponentDeploymentName" \
                                                    --no-prompt true --no-wait \
                                                    --template-file "$ComponentTemplateFile" \
                                                    --parameters "$ComponentTemplateParametersJson" \
                                                    "${ComponentTemplateParametersOpts[@]}" 2>&1)

    fi

    if [ $? -eq 0 ]; then # deployment successfully created
        while true; do

            sleep 1

            ProvisioningState=$(az deployment sub show --name "$ComponentDeploymentName" --query "properties.provisioningState" -o tsv)
            ProvisioningDetails=$(az deployment operation sub list --name "$ComponentDeploymentName")

            trackDeployment "$ProvisioningDetails"
            
            if [[ "CANCELED|FAILED|SUCCEEDED" == *"${ProvisioningState^^}"* ]]; then

                echo -e "\nDeployment $ComponentDeploymentName: $ProvisioningState"

                if [[ "CANCELED|FAILED" == *"${ProvisioningState^^}"* ]]; then
                    exit 1
                else
                    break
                fi
            fi

        done
    fi

else

    if [ "$WebServerEnabled" == "1" ]; then

        ComponentDeploymentOutput=$(az deployment group create --subscription $ComponentSubscription \
                                                               --resource-group "$ComponentResourceGroup" \
                                                               --name "$ComponentDeploymentName" \
                                                               --no-prompt true --no-wait --mode Complete \
                                                               --template-uri "$ComponentTemplateUrl" \
                                                               --query-string "code=$TaskToken" \
                                                               --parameters "$ComponentTemplateParametersJson" \
                                                               "${ComponentTemplateParametersOpts[@]}" 2>&1)

    else

        ComponentDeploymentOutput=$(az deployment group create --subscription $ComponentSubscription \
                                                               --resource-group "$ComponentResourceGroup" \
                                                               --name "$ComponentDeploymentName" \
                                                               --no-prompt true --no-wait --mode Complete \
                                                               --template-file "$ComponentTemplateFile" \
                                                               --parameters "$ComponentTemplateParametersJson" \
                                                               "${ComponentTemplateParametersOpts[@]}" 2>&1)

    fi

    if [ $? -eq 0 ]; then # deployment successfully created
        while true; do

            sleep 1

            ProvisioningState=$(az deployment group show --resource-group "$ComponentResourceGroup" --name "$ComponentDeploymentName" --query "properties.provisioningState" -o tsv)
            ProvisioningDetails=$(az deployment operation group list --resource-group "$ComponentResourceGroup" --name "$ComponentDeploymentName")

            trackDeployment "$ProvisioningDetails"
            
            if [[ "CANCELED|FAILED|SUCCEEDED" == *"${ProvisioningState^^}"* ]]; then

                echo -e "\nDeployment $ComponentDeploymentName: $ProvisioningState"

                if [[ "CANCELED|FAILED" == *"${ProvisioningState^^}"* ]]; then
                    exit 1
                else
                    break
                fi
            fi

        done
    fi

fi

# trim spaces from output to avoid issues in the following (generic) error section
ComponentDeploymentOutput=$(echo "$ComponentDeploymentOutput" | sed -e 's/^[[:space:]]*//')

if [ ! -z "$ComponentDeploymentOutput" ]; then

    if [ $(echo "$ComponentDeploymentOutput" | jq empty > /dev/null 2>&1; echo $?) -eq 0 ]; then
        # the component deployment output was identified as JSON - lets extract some error information to return a more meaningful output
        ComponentDeploymentOutput="$( echo $ComponentDeploymentOutput | jq --raw-output '.. | .message? | select(. != null) | "Error: \(.)\n"' | sed 's/\\n/\n/g'  )"
    fi
    
    # our script failed to enqueue a new deployment -
    # we return a none zero exit code to inidicate this
    echo "$ComponentDeploymentOutput" && exit 1 

fi
    
updateComponentValue