#!/bin/bash

# create directory if it doesn't already exist
mkdir -p build/ 

# run a container to build the gpmf app
docker run --rm -v "$PWD"/../gpmf:/gpmf -w /gpmf/demo gcc:4.9 make
# copy make output binary to local dir
cp ../gpmf/demo/gpmfdemo ./build

# multi-stage build of dotnet container to build and release skiconsole.
docker build -t skiconsole -f Dockerfile . 

####
# Example of running this container, mapping local dir to /share in the container (i.e. to get the MP4):
# docker run -it -v "$PWD":/share skiconsole
####
