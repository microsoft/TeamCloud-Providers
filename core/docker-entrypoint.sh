#!/bin/bash

set -e # exit on error
trap 'catch $? $LINENO' EXIT

catch() {
    if [ "$1" != "0" ]; then
        # we trapped an error - write some reporting output
        error "Exit code $1 was returned from line #$2 !!!"
    fi
}

trace() {
    echo -e "\n>>> $@ ...\n"
}

error() {
    echo "Error: $@" 1>&2
}


# check for mandatory environment data
[[ -z "$TaskId" ]] && error "Missing 'TaskId' environment variable" && exit 1
[[ -z "$TaskHost" ]] && error "Missing 'TaskHost' environment variable" && exit 1

waitForHttp() {
    echo -n "Waiting for http://$TaskHost ."
    until $(curl --output /dev/null --silent --head --fail http://$TaskHost); do
        echo -n '.' && sleep 1
    done
    echo ' done'
}

export -f waitForHttp

waitForHttps() {
    echo -n "Waiting for https://$TaskHost ."
    until $(curl --output /dev/null --silent --head --fail https://$TaskHost); do
        echo -n '.' && sleep 1
    done
    echo ' done'
}

export -f waitForHttps

readonly LOG_FILE="/mnt/storage/.output/$TaskId"

mkdir -p "$(dirname "$LOG_FILE")"                   # ensure the log folder exists
touch $LOG_FILE                                     # ensure the log file exists
exec > >(stdbuf -i0 -oL -eL tee -a $LOG_FILE) 2>&1  # mirror console out to log file

trace "Initialize runner"

# patch nginx configuration with the task host name
sed -i "s/server_name.*/server_name $TaskHost;/g" /etc/nginx/http.d/default.conf

if [[ "$(echo $TaskHost | tr '[:upper:]' '[:lower:]')" != "localhost" ]]; then

    # acquire a ssl certificate to use for web access
    # as certbot is sometimes a little bit picky we
    # covert the SSL request process in a loop covered
    # by a timeout of 5 minutes (worst case scenario)

    echo "Starting web server ..." \
        && nginx -q

    timeout 60 bash -c "waitForHttp" \
        || { error -e " failed" && exit 1 }

    echo "Acquire SSL certificate ..." \
        && for i in $(seq 1 10); do certbot --nginx --register-unsafely-without-email --hsts --agree-tos --quiet -n -d $TaskHost && break || sleep 5; done

    timeout 60 bash -c "waitForHttps" \
        || { error -e " failed" && exit 1 }

    # echo "Probing http://$TaskHost ..." \
    #     && curl -s -o /dev/null -I -w "%{http_code}" http://$TaskHost

    # echo "Probing https://$TaskHost ..." \
    #     && curl -s -o /dev/null -I -w "%{http_code}" https://$TaskHost

fi

# list servernames the host is listening on
echo "Start listening on host: $(nginx -T 2>/dev/null | grep -o "server_name.*" | sed 's/;//' | cut -d ' ' -f2 | sort -u)"

# find entrypoint scripts in alphabetical order to initialize
# the current container instance before we execute the command itself

while read -r f; do
    if [ -x "$f" ]; then 
		. $f # execute each shell script found enabled for execution
	fi 
done < <(find "/docker-entrypoint.d/" -follow -type f -iname "*.sh" -print | sort -n)

# select the directory used as execution context for task command scripts
# this should usually be the directory of the current component if it exists

if [ ! -z "$ComponentTemplateFolder" ] && [ -d "$(echo "$ComponentTemplateFolder" | sed 's/^file:\/\///')" ]; then

	trace "Selecting template directory"
	cd $(echo "$ComponentTemplateFolder" | sed 's/^file:\/\///') && echo $PWD
	
fi	

# the script to execute is defined by the following options
# (the first option matching an executable script file wins)
#
# Option 1: a script path is provided as docker CMD command
# Option 2: a script file following the pattern [TaskType].sh exists in the 
#           current working directory (component definition directory)
# Option 3: a script file following the pattern [TaskType].sh exists in the 
#           /docker-runner.d directory (docker task script directory)

command="$@" 

if [[ -z "$command" ]]; then

	# fall back to task type script
	# if no script was explicitely
	# defined by the CMD parameter

	command="$TaskType.sh"

fi

# try to find a matching task script in the template directory
commandScript="$(find $PWD -maxdepth 1 -iname "$command")"

if [[ -z "$commandScript" ]]; then 

	# try to find a matching task script in the default runner directory
	commandScript="$(find /docker-runner.d -maxdepth 1 -iname "$command")"

else

	# add missing directory context
	commandScript="$PWD/$commandScript"

fi

if [[ ! -z "$commandScript" ]]; then

    # use the found script as command to execute
	trace "Executing '$commandScript'"; ( exec "$commandScript"; exit $? ) || exit $?

elif [[ ! -z "$command" ]]; then

    # execute the command provided for the container instance
    trace "Executing '$command'"; ( exec "$command"; exit $? ) || exit $?

else

    # raise an error as there is no command to execute
	error "Script '$script' does not exist" && exit 1

fi


