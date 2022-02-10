#!/bin/bash

waitForWebServer() {
	while true; do
		STATUS_CODE="$(curl -s -o /dev/null -I -L -m 5 -f -w '%{http_code}' https://$TaskHost/_healthcheck --resolve $TaskHost:443:127.0.0.1)"
		[ "200" == "$STATUS_CODE" ] && echo "Web server starts responding on internal interface" && break 
	done 
	while true; do
		STATUS_CODE="$(curl -s -o /dev/null -I -L -m 5 -f -w '%{http_code}' https://$TaskHost/_healthcheck)"
		[ "200" == "$STATUS_CODE" ] && echo "Web server starts responding on external interface" && break 
	done 
}

export -f waitForWebServer

if [ "$WebServerEnabled" == "1" ]; then

    [ ! -z "$TaskHost" ] \
		&& [ "$(echo $TaskHost | tr '[:upper:]' '[:lower:]')" != "localhost" ] \
		&& trace "Waiting for web server '$TaskHost'" \
		&& timeout 300 bash -c "waitForWebServer" \
		&& echo "Waiting for web server '$TaskHost' took $SECONDS seconds" \
		|| exit $?

fi