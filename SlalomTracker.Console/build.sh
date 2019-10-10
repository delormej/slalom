: ${SKIBLOBS?"Need to set SKIBLOBS env variable."}
#
# This file used for DEBUG build
#
#dotnet restore ./SlalomTracker/SlalomTracker.csproj
docker build -t skiconsole -f ./SlalomTracker.Console/debug.Dockerfile .
#
# Launch debug container
#
# Need to input env variable value here:
#SKIBLOBS=
# Ensure that $SKIBLOBS as an env variable has leading and trailing quotes ("")
docker run -it -v$PWD:/data --rm -e SKIBLOBS="$SKIBLOBS" --name ski-dbg skiconsole /bin/bash
