#!/bin/bash
source prebuild.sh

echo "service::$SKIJOBS_SERVICE"
echo "skiblobs::$SKIBLOBS"

#
# Build .debug container
#
echo "Building DEBUG container."
docker build -t skiwebapi:v7.0 -f ./SlalomTracker.WebApi/debug.Dockerfile . 

#
# Launch debug container
#
docker run --rm --name ski-dbg -p 80:80 -it \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKIJOBS_SERVICE="$SKIJOBS_SERVICE" \
    skiwebapi:v7.0