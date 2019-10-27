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
docker run -it -v$PWD:/data --rm -e SKIBLOBS="$SKIBLOBS" --name ski-dbg skiconsole $PROCESS_ARG
