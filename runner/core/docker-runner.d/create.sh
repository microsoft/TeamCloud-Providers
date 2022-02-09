#!/bin/bash

# this is a default task implementation which in case of
# system commands only writes some informational output.
# if this task should do something you must create your
# own runner container and replace this file with your
# custom task implementation.

TASKFILE=$(basename -- "$0")
TASKNAME="${filename%.*}"
DIR=$(dirname "$0")

echo -e "\nThe task ${TASKNAME^^} is not implemented yet!"
