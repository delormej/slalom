#!/bin/bash
#source prebuild.sh
#source dot.env

#
# Execute this script from the project root!
#


# if ["${PWD##*/}" == "SlalomTracker.WebApi"]
# then
#     echo "ERROR"
    
# fi

REGISTRY=gcr.io/$(gcloud config list --format 'value(core.project)' 2>/dev/null)

# CI/CD could override this version.
if [ -z "$VERSION" ]
then 
    VERSION=$(git describe --abbrev=0 --tag)
fi
container=$REGISTRY/skiwebapi:v$VERSION

echo "skiblobs::$SKIBLOBS"
echo "github_token::$GITHUB_TOKEN"
echo "Building container::$container"

if [ "$1" == "debug" ]; then
    target=" --target build "
    dockerrun="dotnet run -p ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj"
fi

#
# Build container
#
docker build -t $container \
    --build-arg REGISTRY=$REGISTRY \
    --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    --build-arg VERSION=$VERSION \
    --force-rm $target \
    -f ./SlalomTracker.WebApi/Dockerfile . 

#
# Launch debug container... not logging level overridden below to "Info"
#
docker run --rm -p 5000:5000 -it \
    -v "$PWD":/shared \
    -e PORT=5000 \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKISB="$SKISB" \
    -e SKISIGNALR="$SKISIGNALR" \
    -e GITHUB_TOKEN="$GITHUB_TOKEN" \
    -e GOOGLE_APPLICATION_CREDENTIALS="/shared/key.json" \
    -e GOOGLE_PROJECT_ID="$GOOGLE_PROJECT_ID" \
    -e FIRESTORE_PROJECT_ID="$FIRESTORE_PROJECT_ID" \
    -e GOOGLE_STORAGE_BUCKET="$GOOGLE_STORAGE_BUCKET" \
    -e LD_LIBRARY_PATH=/usr/lib \
    -e Logging__LogLevel__Default="Debug" \
    --name ski-web \
    $container $dockerrun

#
# Tag and push the container.
#

if [ "$1" != "debug" ]; then
    docker push $container
fi