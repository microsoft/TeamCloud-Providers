#!/bin/bash

DIR=$(dirname "$0")
TSK="delete.sh"

# check if the task file exists in the current dir
SCRIPT="$(find $PWD -maxdepth 1 -iname "$TSK")"

if [[ -z "$SCRIPT" ]]; then 
    # check if the task file exists in the default runner dir
    SCRIPT="$(find /docker-runner.d -maxdepth 1 -iname "$TSK")"
fi

# isolate task script execution in sub shell  
( exec "$SCRIPT"; exit $? ) || exit $?
