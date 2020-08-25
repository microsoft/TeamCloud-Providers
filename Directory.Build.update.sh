#!/bin/bash

DIRECTORY=`dirname $0`
PROPSFILE="$DIRECTORY/Directory.Build.props"
VERSION=$(curl -s https://api.github.com/repos/microsoft/TeamCloud/releases | jq --raw-output '.[0].name' | sed 's/^v//g')

sed "s|<TeamCloudPackageVersion>.*</TeamCloudPackageVersion>|<TeamCloudPackageVersion>$VERSION</TeamCloudPackageVersion>|g" $PROPSFILE | tee $PROPSFILE && echo ""