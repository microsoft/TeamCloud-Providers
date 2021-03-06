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

readonly LOG_FILE="/mnt/storage/.output/$TaskId"
readonly DMP_FILE="/mnt/storage/value.json"

mkdir -p "$(dirname "$LOG_FILE")"   # ensure the log folder exists
touch $LOG_FILE                     # ensure the log file exists

if [[ ! -z "$TaskHost" ]]; then

    sed -i "s/server_name.*/server_name $TaskHost;/g" /etc/nginx/conf.d/default.conf
    nginx -q # start nginx and acquire SSL certificate from lets encrypt 

    while true; do
        # there is a chance that nginx isn't ready to respond to the ssl challenge - so retry if this operation fails
        certbot --nginx --register-unsafely-without-email --agree-tos --quiet -n -d $TaskHost > /dev/null && break || sleep 1
    done

    # list servernames the host is listening on
    nginx -T 2>/dev/null | grep "server_name " | sort -u
fi

# Redirecting STDOUT and STDERR to our task log must be set
# now and not earlier in the script as NGINX is a littel bit
# picky when it comes to running it in quite mode.

exec 1>$LOG_FILE                    # forward stdout to log file
exec 2>&1                           # redirect stderr to stdout

find "/docker-entrypoint.d/" -follow -type f -iname "*.sh" -print | sort -n | while read -r f; do
    # execute each shell script found enabled for execution
    if [ -x "$f" ]; then trace "Running '$f'"; "$f"; fi
done

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

if [[ ! -z "$ComponentTemplateFolder" ]]; then
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

script="$@" 

if [[ -z "$script" ]]; then
    script="$(find $PWD -maxdepth 1 -iname "$TaskType.sh")"
    if [[ -z "$script" ]]; then 
        script="$(find /docker-runner.d -maxdepth 1 -iname "$TaskType.sh")"
    fi
    if [[ -z "$script" ]]; then 
        error "Deployment type $TaskType is not supported." && exit 1
    fi
fi

if [[ -f "$script" && -x "$script" ]]; then
    # lets execute the task script - isolate execution in sub shell 
    trace "Executing script ($script)"; ( exec "$script"; exit $? ) || exit $?
elif [[ -f "$script" ]]; then
    error "Script '$script' is not marked as executable" && exit 1
else
    error "Script '$script' does not exist" && exit 1
fi

trace "Update component value"
if [ -z "$ComponentResourceGroup" ]; then
    az resource list --subscription $ComponentSubscription > $DMP_FILE
else
    az resource list --subscription $ComponentSubscription -g $ComponentResourceGroup > $DMP_FILE
fi
if [ -f "$DMP_FILE" ]; then
    echo "$DMP_FILE ($(stat -c%s "$DMP_FILE") bytes)"
fi