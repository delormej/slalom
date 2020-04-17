#!/bin/bash
source prebuild.sh

echo "service::$SKIJOBS_SERVICE"
echo "skiblobs::$SKIBLOBS"
echo "github_token::$GITHUB_TOKEN"

container=skiwebapi:debug

#
# Build .debug container
#
echo "Building DEBUG container."
docker build -t $container --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    -f ./SlalomTracker.WebApi/debug.Dockerfile . 

# Production build:
#docker build -t skiweb:v<!PUT VERSION HERE!> --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
#    -f ./SlalomTracker.WebApi/Dockerfile .

#
# Launch debug container... not logging level overridden below to "Info"
#
docker run --rm --name ski-dbg -p 80:80 -it \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKIJOBS_SERVICE="$SKIJOBS_SERVICE" \
    -e Logging__LogLevel__Default="Debug" \
    $container