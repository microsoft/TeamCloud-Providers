#!/bin/bash
DIR=$(dirname "$0")

trace() {
    echo -e "\n>>> $@ ...\n"
}


# trace "Deleting deployments"
# kubectl delete deployments --all --namespace 