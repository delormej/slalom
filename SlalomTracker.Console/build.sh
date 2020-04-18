#!/bin/bash
source prebuild.sh
echo "github_token::$GITHUB_TOKEN"
#
# Build .debug container
#
echo "Building DEBUG container."
docker build -t skiconsole:debug --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    -f ./SlalomTracker.Console/Dockerfile .

#
# Launch debug container
#
# Mounting volume is no longer working? -v$PWD:/data 
docker run -it --rm -e SKIBLOBS="$SKIBLOBS" \
    -e GOOGLE_APPLICATION_CREDENTIALS="/ski/gcloud.json" --name ski-dbg skiconsole:debug $PROCESS_ARG
