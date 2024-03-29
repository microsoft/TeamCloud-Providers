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

echod() {
    if [ "$DebugMode" == "1" ]; then echo $@; fi
}

# check for mandatory mounted volumes
[[ ! -d "/mnt/templates" ]] && error "Missing volume '/mnt/templates'" && exit 1
[[ ! -d "/mnt/storage" ]] && error "Missing volume '/mnt/storage'" && exit 1
[[ ! -d "/mnt/secrets" ]] && error "Missing volume '/mnt/secrets'" && exit 1
[[ ! -d "/mnt/credentials" ]] && error "Missing volume '/mnt/credentials'" && exit 1

# check for mandatory environment variables
[[ -z "$TaskId" ]] && error "Missing 'TaskId' environment variable" && exit 1
[[ -z "$TaskHost" ]] && error "Missing 'TaskHost' environment variable" && exit 1

readonly LOG_FILE="/mnt/storage/.output/$TaskId"

mkdir -p "$(dirname "$LOG_FILE")" && touch $LOG_FILE 	# ensure the log folder and file exists
exec &> >(unbuffer -p tee -ia $LOG_FILE)    			# mirror STDOUT & STDERR to log file (with buffering disabled) - unbuffer-version

trace "TeamCloud Runner Info" && printenv | grep '^TCRUNNER_*' | cat

# find entrypoint scripts in alphabetical order to initialize
# the current container instance before we execute the command itself

while read -r f; do
    if [ -x "$f" ]; then 
        echod "Executing entrypoint script: $f"
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


