#!/bin/bash

# create directory if it doesn't already exist
mkdir -p build/ 

# run a container to build the gpmf app
docker run --rm -v "$PWD"/../gpmf:/gpmf -w /gpmf/demo gcc:4.9 make
# copy make output binary to local dir
cp ../gpmf/demo/gpmfdemo ./build

# multi-stage build of dotnet container to build and release skiconsole.
docker build -t skiconsole  --build-arg ski_blobs_connection="$SKIBLOBS" -f Dockerfile . 

####
# Example of running this container, mapping local dir to /share in the container (i.e. to get the MP4):
# docker run -it -v "$PWD":/share skiconsole
####

####
# Deploying to Azure Container Instance:
# https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-azure-cli
# 0. Tag docker image: docker tag skiconsole jasondelacr.azurecr.io/skiconsole:v1
# 1. Create Azure Container Registry
# 2. $ docker login jasondelacr.azurecr.io -u jasondelAcr -p XXXXXXX
#        (user admin user and password enabled when you use --enable-admin=true during ACR create)
# 3. docker push jasondelacr.azurecr.io/skiconsole:v1
# 4. Create the aci:
#       az container create --resource-group MyAcrGroup --name aci-skiconsole --image jasondelacr.azurecr.io/skiconsole:v1 --cpu 1 --memory 1 --registry-usernamejasondelAcr --registry-password XXXXXX --dns-name-label aci-skiconsole --ports 5000
#
# Need to configure webapp (even for azure function) to use private Azure Container Registry
# az webapp config container set --name <app_name> --resource-group myResourceGroup --docker-custom-image-name <azure-container-registry-name>.azurecr.io/mydockerimage --docker-registry-server-url https://<azure-container-registry-name>.azurecr.io --docker-registry-server-user <registry-username> --docker-registry-server-password <password>