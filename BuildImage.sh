#!/bin/bash

set -e # exit on error
clear && csd=$(dirname "$0")

if [ -z "$1" ]; then
	echo "Provide the name of a image folder as parameter"
	exit 1
fi

header() {
	echo -e "\n========================================================================================================================="
	echo -e $1
	echo -e "-------------------------------------------------------------------------------------------------------------------------\n"
}

pushd $csd/$1 > /dev/null

TIMESTAMP="$(date +%s)"

TEAMCLOUD_REGISTRY_SUBSCRIPTION="12223725-70b0-45a6-96c4-a13c344fdc57"
TEAMCLOUD_REGISTRY_NAME="TeamCloud"
TEAMCLOUD_REGISTRY_LOGINSERVER=$(az acr show --subscription $TEAMCLOUD_REGISTRY_SUBSCRIPTION --name $TEAMCLOUD_REGISTRY_NAME --query loginServer -o tsv)
TEAMCLOUD_IMAGE_TAGPREFIX="$TEAMCLOUD_REGISTRY_LOGINSERVER/teamcloud-dev/tcrunner-$(basename "$PWD" | tr '[:upper:]' '[:lower:]')-$(whoami)"
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

ENV_TASKID=$(uuidgen)

header "Deleting existing containers ..."
docker container rm -f $(docker container ls -a -q) || true

header "Run new docker image ..."
(set -x; docker run \
	-it -P \
	--mount type=tmpfs,destination=/mnt/templates \
	--mount type=tmpfs,destination=/mnt/storage \
	--env TaskId=$ENV_TASKID \
	--env TaskHost=localhost \
	--env ComponentTemplateRepository="https://github.com/markusheiliger/TeamCloud-Project-Sample.git" \
	$TEAMCLOUD_IMAGE_TAGLATEST \
	"debug.sh")
