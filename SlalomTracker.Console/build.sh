#!/bin/bash
source prebuild.sh

#
# Build .debug container
#
echo "Building DEBUG container."
docker build -t skiconsole -f ./SlalomTracker.Console/debug.Dockerfile .

#
# Launch debug container
#
# Mounting volume is no longer working? -v$PWD:/data 
docker run -it --rm -e SKIBLOBS="$SKIBLOBS" \
    -e GOOGLE_APPLICATION_CREDENTIALS="/ski/gcloud.json" --name ski-dbg skiconsole $PROCESS_ARG
