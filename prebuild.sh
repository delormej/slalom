#!/bin/bash
DOTENV=dev.env
if test -f "$DOTENV"; then
    echo "Loading dev.env"
    source $DOTENV
else
    SKIBLOBS=$(az storage account show-connection-string --name skivideostorage --query connectionString)
fi

: ${SKIBLOBS?"Need to set SKIBLOBS env variable or dev.env file with the value."}

# Set subscription 
export SUBSCRIPTIONID=$(az account show -o tsv --query id) 

# If an argument is passed, use it as a video.
if [ $# -eq 0 ]
  then
    echo "No argument supplied for ./ski -p, just building."
    PROCESS_ARG=/bin/bash
  else
    PROCESS_ARG="./ski -p $1"
fi

#
# This file used for DEBUG build
#
dotnet build ./SlalomTracker/SlalomTracker.csproj