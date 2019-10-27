#!/bin/bash
source prebuild.sh

# This needs to be run from source root (down a directory)

docker build -t skiwebapi:v7.0 -f ./SlalomTracker.WebApi/Dockerfile . 
docker run --rm --name ski-web-dbg -p 80:80 -it -e SKIBLOBS="$SKIBLOBS" skiwebapi:v7.0
