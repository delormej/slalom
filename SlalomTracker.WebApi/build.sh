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

#
# Launch debug container
#
docker run --rm --name ski-dbg -p 80:80 -it \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKIJOBS_SERVICE="$SKIJOBS_SERVICE" \
    $container