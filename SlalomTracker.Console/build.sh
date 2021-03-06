#!/bin/bash
#source prebuild.sh

REGISTRY=gcr.io/$(gcloud config list --format 'value(core.project)' 2>/dev/null)

# CI/CD could override this version.
# This will get the latest short commit hash: $(git rev-parse --short HEAD)
if [ -z "$VERSION" ]
then 
    VERSION=$(git describe --abbrev=0 --tags)
fi
container=$REGISTRY/skiconsole:v$VERSION

echo "skisb::$SKISB"
echo "skiblobs::$SKIBLOBS"
echo "github_token::$GITHUB_TOKEN"
echo "skimlkey::$SKIMLKEY"
echo "Building container::$container"

if [ "$1" == "debug" ]; then
    target=" --target build "
fi

#
# Build container
#
echo "Building container."
docker build -t $container \
    --build-arg REGISTRY=$REGISTRY \
    --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    --build-arg VERSION=$VERSION \
      --force-rm $target \
    -f ./SlalomTracker.Console/Dockerfile .
#
# To just use the debug image add --target build to the above and it won't build the release stage.
#

#
# Launch container
#
docker run -it --rm \
    -v "$PWD":/shared \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKISB="$SKISB" \
    -e SKIQUEUE="$SKIQUEUE" \
    -e SKIMLKEY="$SKIMLKEY" \
    -e GOOGLE_APPLICATION_CREDENTIALS="/ski/key.json" \
    -e GITHUB_TOKEN="$GITHUB_TOKEN" \
    -e SKIMLROPEMODEL="RopeDetection-4" \
    -e SKIMLROPEID="e3ee86a8-f298-46b5-87fd-31a09f0480d7" \
    -e SKIMLSKIERID="c38bd611-86ee-43ff-ad76-20d339665e34" \
    -e SKIMLSKIERMODEL="SkierDetection-2" \
    -e FIRESTORE_PROJECT_ID="$FIRESTORE_PROJECT_ID" \
    -e GOOGLE_PROJECT_ID="$GOOGLE_PROJECT_ID" \
    -e GOOGLE_STORAGE_BUCKET="$GOOGLE_STORAGE_BUCKET" \
    -e SKICROPURL="$SKICROPURL" \
    --name ski-console \
    --cpus="2.0" \
    $container

# az acr login -n wthacr
if [ "$1" != "debug" ]; then
    docker push $container
fi

#
# Script to get message counts from Service Bus
#
#az servicebus queue show -g jasondel-aks --namespace-name jasondel-skivideos --name video-uploaded --query "countDetails"
# gsutil cors set CORS.json gs://$GOOGLE_STORAGE_BUCKET