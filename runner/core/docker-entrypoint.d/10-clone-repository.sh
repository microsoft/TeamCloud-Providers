#!/bin/bash

# CAUTION - this script will only be executed if the templates volumne isn't containing any data!
# As long as the container instance is hosted in ACI this script won't do anything. However, if 
# TeamCloud will switch over to Kubernetes as a container runtime this script is needed to make
# the templates repository available in the runner container for task execution!

if [ ! -z "$ComponentTemplateRepository" ] && [ -d "/mnt/templates" ] && [ -z "$(ls -A /mnt/templates)" ]; then

	trace "Checkout templates"
	git clone "$ComponentTemplateRepository" /mnt/templates

fi
