#!/bin/bash

KUBECONFIG="/mnt/credentials/password"

echo "HERE WE ARE"
[ -f "$KUBECONFIG" ] && mkdir -p ~/.kube && cp --verbose $KUBECONFIG ~/.kube/config

echo "HERE WE ARE AGAIN"
cat ~/.kube/config

