#!/bin/bash

trackDeployment() { 

    trace="$( echo "$1" | jq --raw-output '.[] | [.operationId, .properties.timestamp, .properties.provisioningOperation, .properties.provisioningState, .properties.targetResource.id // ""] | @tsv' )"
    
    echo "$trace" | while read -r line; do 
        if [[ ! -z "$line" ]]; then

            operationId="$( echo "$line" | cut -f 1 )"
            operationTimestamp="$( echo "$line" | cut -f 2 | cut -d . -f 1 | sed 's/T/ /g' )"
            operationType="$( echo "$line" | cut -f 3 )"
            operationState="$( echo "$line" | cut -f 4 )"
            operationTarget="$( echo "$line" | cut -f 5 )"
            operationHash="$( echo "$operationId|$operationState" | md5sum | cut -d ' ' -f 1 )"

            if ! grep -q "$operationHash" /tmp/hashes 2>/dev/null ; then

                echo -e "\n$operationTimestamp\t$operationId - $operationType ($operationState)"
                
                if [[ ! -z "$operationTarget" ]]; then
                    echo -e "\t\t\t$operationTarget"
                fi

                echo "$operationHash" >> /tmp/hashes

            fi
        fi
    done
}