#!/bin/bash

if [ ! -z "$ComponentTemplateRepository" ] && [ -d "/mnt/templates" ] && [ -z "$(ls -A /mnt/templates)" ]; then

	trace "Checkout templates"
	git clone "$ComponentTemplateRepository" /mnt/templates

fi
