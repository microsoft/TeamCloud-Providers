#!/bin/bash

waitForAzureConnection() {
	while true; do
		az login --identity --allow-no-subscriptions --only-show-errors --output none 2> /dev/null && {
			export ARM_USE_MSI=true
			export ARM_MSI_ENDPOINT='http://169.254.169.254/metadata/identity/oauth2/token'
			export ARM_SUBSCRIPTION_ID=$ComponentSubscription
			break
		} || sleep 5    
	done 
}

export -f waitForAzureConnection

[ ! -z "$ComponentSubscription" ] \
	&& trace "Connecting Azure" \
	&& timeout 300 bash -c "waitForAzureConnection" \
	&& az ad signed-in-user show \
	&& trace "Selecting Subscription" \
	&& az account set --subscription $ComponentSubscription \
	&& echo "$(az account show -o json | jq --raw-output '"\(.name) (\(.id))"')"
