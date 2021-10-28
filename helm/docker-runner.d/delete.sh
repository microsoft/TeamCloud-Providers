#!/bin/bash
DIR=$(dirname "$0")
. $DIR/_common.sh

trace() {
    echo -e "\n>>> $@ ...\n"
}


# trace "Deleting deployments"
# kubectl delete deployments --all --namespace 