#!/bin/bash
pushd
source prebuild.sh
popd

#
# Build .debug container
#
echo "Building DEBUG container."
docker build -t skijobsapi -f ./Dockerfile . 

#
# Launch debug container
#
docker run -v$HOME:/data --entrypoint /bin/bash --rm --name ski-dbg -p 80:80 -it -v --env-file=dev.env \
    skijobsapi