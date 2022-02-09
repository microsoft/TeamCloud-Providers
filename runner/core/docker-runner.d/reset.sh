#!/bin/bash

function ResolveScript () {

    # check if the task file exists in the process dir
    SCRIPT="$(find $PWD -maxdepth 1 -iname "$1")"

    if [[ -z "$SCRIPT" ]]; then 
        # check if the task file exists in the default runner dir
        SCRIPT="$(find /docker-runner.d -maxdepth 1 -iname "$1")"
    fi

    echo "$SCRIPT"
}

# isolate task script execution in sub shell  
( exec "$( ResolveScript 'delete.sh' )"; exit $? ) && ( exec "$( ResolveScript 'create.sh' )"; exit $? ) || exit $?
