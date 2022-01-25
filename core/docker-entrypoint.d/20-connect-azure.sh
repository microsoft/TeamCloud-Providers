#!/bin/bash

trace "Connecting Azure"

# check if the MSI token endpoint is available - we are interested in the returned status code only 
MSI_STATUSCODE="$(curl -s -o /dev/null -w "%{http_code}" -H Metadata:true 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fmanagement.azure.com%2F')"

if [ "200" == "$MSI_STATUSCODE" ]; then

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

else

	error "Unable to connect MSI authenication endpoint (status code $MSI_STATUSCODE)"

fi

