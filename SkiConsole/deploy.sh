#!/bin/bash

#
# Need to build a container for the build with:
#   shared volume
#   dotnet sdk, build-essentials (gcc, make, etc...)
#   clone the repo (or copy assets? )
#   run the build
#  
# After this container completes, 
# create another container with the resulting artifacts from the shared volume
#

# microsoft/dotnet:2.1-aspnetcore-runtime
#deploy.sh
cd ../../gpmf/demo/
make
mv ./gpmfdemo ../../slalom/SkiConsole/
cd ../../slalom/SkiConsole/
dotnet build ./SkiConsole.csproj
dotnet publish 
# make a copy of the MP4 file for testing
cp *.MP4 ./bin/Debug/netcoreapp2.1/publish/
docker build -t skiconsole -f Dockerfile ./bin/Debug/netcoreapp2.1/publish/ 
