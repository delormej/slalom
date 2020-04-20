#!/bin/bash
# For some reason, I just can't get these following variables to populate calling ./prebuild.
#source ./prebuild.sh
echo "github_token::$GITHUB_TOKEN"
echo "skiblobs::$SKIBLOBS"
echo "skisb::$SKISB"
exit
VERSION=v5.0.0
#
# Build container
#
echo "Building container."
docker build -t skiconsole:$VERSION --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    -f ./SlalomTracker.Console/Dockerfile .
#
# To just use the debug image add --target build to the above and it won't build the release stage.
#

#
# Launch container
#
# env variable skiblobs still not working...
docker run -it --rm \
    -v $PWD:/shared \
    -e SKIBLOBS='$SKIBLOBS' \
    -e SKISB='$SKISB' \
    -e GOOGLE_APPLICATION_CREDENTIALS="/ski/gcloud.json" \
    --name ski-dbg \
    skiconsole:$VERSION 

docker tag skiconsole:$VERSION wthacr.azurecr.io/skiconsole:$VERSION
docker push wthacr.azurecr.io/skiconsole:$VERSION