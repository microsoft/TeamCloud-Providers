#!/bin/bash

waitForWebServer() {
    until [ $(curl --output /dev/null --max-time 1 --silent --head --fail http://$TaskHost) ]; do
        echo -n '.'
        sleep 1
    done
}

export -f waitForWebServer

if [ "$EnableWebServer" == "1" ] \
	&& [ ! -z "$TaskHost" ] \
	&& [ "$(echo $TaskHost | tr '[:upper:]' '[:lower:]')" != "localhost" ]; then

	# update nginx config to listen on the public TaskHost name
    sed -i "s/server_name.*/server_name $TaskHost;/g" /etc/nginx/http.d/default.conf

    echo -n "Starting web server ..." \
        && nginx -q 

    timeout 60 bash -c "waitForWebServer" \
        && echo " done" || { echo " failed" && exit 1; }

    echo "Acquire SSL certificate ..." \
        && for i in $(seq 1 10); do certbot --nginx --register-unsafely-without-email --hsts --agree-tos --quiet -n -d $TaskHost && { echo "done" && break; } || sleep 5; done 


	echo "Start listening on host: $(nginx -T 2>/dev/null | grep -o "server_name.*" | sed 's/;//' | cut -d ' ' -f2 | sort -u)"

fi