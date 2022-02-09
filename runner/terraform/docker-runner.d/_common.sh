#!/bin/bash

updateComponentValue() {

    local DMP_FILE="/mnt/storage/value.json"

    trace "Update component value"
    
    if [ -z "$ComponentResourceGroup" ]; then
        az resource list --subscription $ComponentSubscription > $DMP_FILE
    else
        az resource list --subscription $ComponentSubscription -g $ComponentResourceGroup > $DMP_FILE
    fi
    
    if [ -f "$DMP_FILE" ]; then
        echo "$DMP_FILE ($(stat -c%s "$DMP_FILE") bytes)"
    fi

}