#!/bin/bash

VERSION=$(curl -s https://api.github.com/repos/microsoft/TeamCloud/releases | jq --raw-output '.[0].name' | sed 's/^v//g')
DIRECTORY=`dirname $0`
PROPSFILE="$DIRECTORY/Directory.Build.props"
PROPSXML=$(sed -i "s|<TeamCloudPackageVersion>.*<|<TeamCloudPackageVersion>$VERSION<|" $PROPSFILE)

if [ ! -z "$PROPSXML" ]; then
    echo "$PROPSXML" > $PROPSFILE
fi

cat $PROPSFILE
