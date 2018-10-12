#!/bin/bash
# microsoft/dotnet:2.1-aspnetcore-runtime
#deploy.sh
cd ../../gpmf/demo/
make
mv ./gpmfdemo ../../slalom/SkiConsole/
cd ../../slalom/SkiConsole/
dotnet build ./SkiConsole.csproj
