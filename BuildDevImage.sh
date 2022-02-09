#!/bin/bash

set -e # exit on error
clear && csd=$(dirname "$0")

header() {
	echo -e "\n========================================================================================================================="
	echo -e $1
	echo -e "-------------------------------------------------------------------------------------------------------------------------\n"
}

if [ -z "$1" ]; then
	echo "Provide the name of a image folder as parameter"
	exit 1
fi

path=$(find . -type d -iname "$1" -print 2>/dev/null)

if [ -z "$path" ]; then
	echo "Could not find folder by name '$1'"
	exit 1
fi

pushd $csd/$path > /dev/null

TIMESTAMP="$(date +%s)"

TEAMCLOUD_REGISTRY_SUBSCRIPTION="b6de8d3f-8477-45fe-8d60-f30c6db2cb06"
TEAMCLOUD_REGISTRY_NAME="TeamCloud"
TEAMCLOUD_REGISTRY_LOGINSERVER=$(az acr show --subscription $TEAMCLOUD_REGISTRY_SUBSCRIPTION --name $TEAMCLOUD_REGISTRY_NAME --query loginServer -o tsv)
TEAMCLOUD_IMAGE_TAGPREFIX="$TEAMCLOUD_REGISTRY_LOGINSERVER/teamcloud-dev/tc$(basename "$(dirname "$PWD")" | tr '[:upper:]' '[:lower:]')-$(basename "$PWD" | tr '[:upper:]' '[:lower:]')-$(whoami)"
TEAMCLOUD_IMAGE_TAGLATEST="$TEAMCLOUD_IMAGE_TAGPREFIX:latest"
TEAMCLOUD_IMAGE_TAGTIMESTAMP="$TEAMCLOUD_IMAGE_TAGPREFIX:$TIMESTAMP"

header "Login to registry $TEAMCLOUD_REGISTRY_LOGINSERVER ..."
[ "$(uname -r | sed -n 's/.*\( *[M|m]icrosoft *\).*/\1/p' | tr '[:upper:]' '[:lower:]')" == "microsoft" ] \
	&& echo $(jq 'del(.credsStore)' ~/.docker/config.json) > ~/.docker/config.json
az acr credential show --subscription $TEAMCLOUD_REGISTRY_SUBSCRIPTION --name $TEAMCLOUD_REGISTRY_NAME --query passwords[0].value -o tsv | \
	docker login --username $(az acr credential show --subscription $TEAMCLOUD_REGISTRY_SUBSCRIPTION --name $TEAMCLOUD_REGISTRY_NAME --query username -o tsv) --password-stdin $TEAMCLOUD_REGISTRY_LOGINSERVER

header "Deleting obsolete images ..."
if [ "$2" == "cleanup" ]; then
 	docker image rm -f $(docker image ls -q $TEAMCLOUD_IMAGE_TAGPREFIX) 2> /dev/null || true
else
	docker image rm -f $TEAMCLOUD_IMAGE_TAGLATEST 2> /dev/null || true
fi

header "Building docker image in folder $PWD ..."
if [ "$2" == "cleanup" ]; then
	DOCKER_BUILDKIT=1 docker build ${@:3} --tag $TEAMCLOUD_IMAGE_TAGLATEST --tag $TEAMCLOUD_IMAGE_TAGTIMESTAMP .
else
	DOCKER_BUILDKIT=1 docker build ${@:2} --tag $TEAMCLOUD_IMAGE_TAGLATEST --tag $TEAMCLOUD_IMAGE_TAGTIMESTAMP .
fi

header "Pushing docker image $TEAMCLOUD_IMAGE_TAGPREFIX with tag 'latest' and '$TIMESTAMP' to registry ..."
docker push --all-tags $TEAMCLOUD_IMAGE_TAGPREFIX

header "List docker images for repository $TEAMCLOUD_IMAGE_TAGPREFIX ..."
docker images $TEAMCLOUD_IMAGE_TAGPREFIX

popd > /dev/null

header "Deleting existing containers ..."
docker container rm -f $(docker container ls -a -q) 2> /dev/null || true

# baSH