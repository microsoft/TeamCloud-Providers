#!/bin/bash

trace() {
    echo -e "\n>>> $@ ...\n"
}

trace "DEBUG Environment"
printenv
