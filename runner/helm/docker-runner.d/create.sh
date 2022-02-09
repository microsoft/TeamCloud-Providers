#!/bin/bash
DIR=$(dirname "$0")

trace() {
    echo -e "\n>>> $@ ...\n"
}

trace "Kubernetes config" 
kubectl config view
