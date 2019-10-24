: ${SKIBLOBS?"Need to set SKIBLOBS env variable."}

# If an argument is passed, use it as a video.
if [ $# -eq 0 ]
  then
    echo "No arguments supplied"
    PROCESS_ARG=/bin/bash
  else
    PROCESS_ARG="./ski -p $1"
fi

echo $PROCESS_ARG

#
# This file used for DEBUG build
#
#dotnet restore ./SlalomTracker/SlalomTracker.csproj
docker build -t skiconsole -f ./SlalomTracker.Console/Dockerfile .
#
# Launch debug container
#
# Need to input env variable value here:
#SKIBLOBS=
# Ensure that $SKIBLOBS as an env variable has leading and trailing quotes ("")
docker run -it -v$PWD:/data --rm -e SKIBLOBS="$SKIBLOBS" --name ski-dbg skiconsole $PROCESS_ARG
