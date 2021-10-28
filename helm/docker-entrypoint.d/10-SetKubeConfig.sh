#!/bin/bash

KUBECONFIG="/mnt/credentials/password"

[ -f "$KUBECONFIG" ] && cp $KUBECONFIG ~/.kube/config
