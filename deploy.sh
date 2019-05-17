#!/bin/bash

: ${SKIBLOBS?"Need to set SKIBLOBS environment variable"}
: ${1?"Specify version tag"}

# multi-stage build of dotnet container to build and release web api.
docker build -t jasondelacr.azurecr.io/skiwebapi:$1 --build-arg ski_blobs_connection="$SKIBLOBS" -f ./SlalomTracker.WebApi/Dockerfile .

# build Azure Function container.
#docker build -t jasondelacr.azurecr.io/skivideofunction:$1 --build-arg ski_blobs_connection="$SKIBLOBS" -f SlalomTracker.OnVideoQueued/Dockerfile .

# Authenticate to the Azure Container Registry
az acr login -n jasondelAcr

# push to the Azure Container Registry
docker push jasondelacr.azurecr.io/skiwebapi:$1
#docker push jasondelacr.azurecr.io/skivideofunction:$1

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

# Note: deploying to Azure Web App uses port 80 regardless, so you need to: az webapp config appsettings set WEBSITES_PORT 5000
# https://docs.microsoft.com/en-us/azure/app-service/containers/tutorial-custom-docker-image
# ski-app.azurewebsites.net
#
# Need to configure webapp (even for azure function) to use private Azure Container Registry
# az webapp config container set --name <app_name> --resource-group myResourceGroup --docker-custom-image-name <azure-container-registry-name>.azurecr.io/mydockerimage --docker-registry-server-url https://<azure-container-registry-name>.azurecr.io --docker-registry-server-user <registry-username> --docker-registry-server-password <password>

# To create the azure function app
#az functionapp create --name skivideofunction --storage-account  skivideostorage  --resource-group ski --plan ski-linux-appservice --deployment-container-image-name jasondelacr.azurecr.io/skivideofunction:$1

