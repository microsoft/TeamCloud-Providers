#!/bin/bash

KUBECONFIG="/mnt/credentials/password"

[ -f "$KUBECONFIG" ] && \
	mkdir -p ~/.kube && \
	cp $KUBECONFIG ~/.kube/config && \
	chmod go-r ~/.kube/config
