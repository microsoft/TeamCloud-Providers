#!/bin/bash

if [ "200" == "$(curl -s -o /dev/null -w "%{http_code}" http://169.254.169.254/metadata/identity/oauth2/token)" ]; then

	trace "Connecting Azure"
	while true; do

		# managed identity isn't available directly 
		# we need to do retry after a short nap
		az login --identity --allow-no-subscriptions --only-show-errors --output none && {
			export ARM_USE_MSI=true
			export ARM_MSI_ENDPOINT='http://169.254.169.254/metadata/identity/oauth2/token'
			export ARM_SUBSCRIPTION_ID=$ComponentSubscription
			echo "done"
			break
		} || sleep 5    

	done

	if [[ ! -z "$ComponentSubscription" ]]; then

		trace "Selecting Subscription"
		az account set --subscription $ComponentSubscription
		echo "$(az account show -o json | jq --raw-output '"\(.name) (\(.id))"')"

	fi

fi

