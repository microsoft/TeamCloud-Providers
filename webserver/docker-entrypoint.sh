#!/bin/bash
set -e # break on error

header() {
	echo -e "\n========================================================================================================================="
	echo -e $1
	echo -e "-------------------------------------------------------------------------------------------------------------------------\n"
}

waitForNginx() {

	[ ! -f /var/run/nginx.pid ] \
		&& echo "ERROR nginx is not running" \
		&& exit 1

	NGINX_PID="$(cat /var/run/nginx.pid)"
	LISTEN_PID="$(netstat -tulpn | grep :80 | sed 's/  */ /g' | cut -d ' ' -f 7 | cut -d '/' -f 1)"
	
	[ "$NGINX_PID" != "$LISTEN_PID" ] \
		&& echo "ERROR nginx is not listening on port 80" \
		&& exit 1
	
	while true; do
		[ "000" != "$(curl -s -o /dev/null -I -L -m 2 -f -w '%{http_code}' http://$TaskHost --resolve $TaskHost:80:127.0.0.1)" ] && break || true
	done 

	if [ "localhost" != "$(echo "$TaskHost" | tr '[:upper:]' '[:lower:]')" ]; then
		while true; do

			PUBLIC_NS=""
			PUBLIC_IP=""

			while read NAMESERVER; do

				PUBLIC_NS="$NAMESERVER"
				PUBLIC_IP="$(dig +short @$PUBLIC_NS $TaskHost)"
				
				[ ! -z "$PUBLIC_IP" ] && break
				
			done < <(dig ns +short $(echo $TaskHost | cut -d "." -f 2-) | sed 's/\.$//')

			STATUS_CODE=""

			if [ -z "$PUBLIC_IP" ]; then
				echo "Unable to resolve IP for $TaskHost - use default DNS resolution as fallback"
				STATUS_CODE="$(curl -s -o /dev/null -I -L -m 5 -f -w '%{http_code}' http://$TaskHost)"
			else
				echo "Resolved $PUBLIC_IP for $TaskHost (@$PUBLIC_NS)"
				STATUS_CODE="$(curl -s -o /dev/null -I -L -m 5 -f -w '%{http_code}' http://$TaskHost --resolve $TaskHost:80:$PUBLIC_IP)"
			fi

 			[ "200" == "$STATUS_CODE" ] && echo "done ($STATUS_CODE)" && break || echo "retry ($STATUS_CODE)"
		done 
	fi
}

export -f waitForNginx

waitForCertbot() {

	while true; do
		certbot --nginx --register-unsafely-without-email --agree-tos -v -n -d $TaskHost && break || sleep 5
	done
}

export -f waitForCertbot

header "Initialize Nginx configuration" && tee /etc/nginx/conf.d/default.conf <<EOF

map \$request_uri \$is_unauthorized {
    default				1;
    ~code=$TaskToken 	0;
}

server {

    listen       80;
    server_name  $TaskHost;

	access_log   /dev/null;
	error_log    /dev/null;

    location / {
        root        /usr/share/nginx/html; 
		index 		index.html;
    }
}

EOF

header "Starting host '$TaskHost' ..." \
	&& nginx -q \
	&& timeout 300 bash -c "waitForNginx" \
	|| exit 1

header "Acquire SSL certificate for host '$TaskHost' ..." \
	&& timeout 120 bash -c "waitForCertbot" \
	|| exit 1

LOCATION_BLOCK="$( { cat | sed -z 's/\n/\\n/g' | sed -E 's/\{/\\\{/g' | sed -E 's/\}/\\\}/g' | sed -E 's/\//\\\//g' | sed -E 's/\t/    /g'; } <<EOF

	location / {
		root 		/mnt/templates;
		autoindex   on;
		
		if (\$is_unauthorized) { 
			return 403; 
		}
	}

	location /_healthcheck {
		return 200 'healthy';
		add_header Content-Type text/plain;
	}

EOF
)"

header "Updating Nginx configuration" \
	&& sed -zri "s/\s*location\s+\/\s+\{[^}]*\}/$LOCATION_BLOCK/g" /etc/nginx/conf.d/default.conf \
	&& cat /etc/nginx/conf.d/default.conf \
	&& nginx -s reload \
	&& tail -f /dev/null

