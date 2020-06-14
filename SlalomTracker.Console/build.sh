#!/bin/bash
#source prebuild.sh

# CI/CD could override this version.
# This will get the latest short commit hash: $(git rev-parse --short HEAD)
if [ -z "$VERSION" ]
then 
    VERSION=$(git describe --abbrev=1 --tags)
fi
container=skiconsole:v$VERSION

echo "skisb::$SKISB"
echo "skiblobs::$SKIBLOBS"
echo "github_token::$GITHUB_TOKEN"
echo "skimlkey::$SKIMLKEY"
echo "Building container::$container"

#
# Build container
#
echo "Building container."
docker build -t $container --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    --build-arg VERSION=$VERSION \
    --target build \
    --force-rm \
    -f ./SlalomTracker.Console/Dockerfile .
#
# To just use the debug image add --target build to the above and it won't build the release stage.
#

#
#
# Launch container
# env variable skiblobs still not working...
docker run -it --rm \
    -v "$PWD":/shared \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKISB="$SKISB" \
    -e SKIMLKEY="$SKIMLKEY" \
    -e GOOGLE_APPLICATION_CREDENTIALS="/ski/gcloud.json" \
    --name ski-console \
    --cpus="2.0" \
    $container

# az acr login -n wthacr
# docker tag $container wthacr.azurecr.io/$container
# docker push wthacr.azurecr.io/$container

#
# Script to get message counts from Service Bus
#
#az servicebus queue show -g jasondel-aks --namespace-name jasondel-skivideos --name video-uploaded --query "countDetails"