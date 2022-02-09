#!/bin/bash

waitForWebServer() {
	while true; do
		[ "200" == "$(curl -s -o /dev/null -I -L -m 5 -f -w '%{http_code}' https://$TaskHost/_healthcheck)" ] && break 
	done 
}

export -f waitForWebServer

[ "$WebServerEnabled" == "1" ] \
    && [ ! -z "$TaskHost" ] \
    && [ "$(echo $TaskHost | tr '[:upper:]' '[:lower:]')" != "localhost" ] \
	&& trace "Waiting for web server" \
    && timeout 300 bash -c "waitForWebServer" 