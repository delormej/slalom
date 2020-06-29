#!/bin/bash
#source prebuild.sh

# CI/CD could override this version.
if [ -z "$VERSION" ]
then 
    VERSION=$(git describe --abbrev=0 --tag)
fi
container=skiwebapi:v$VERSION

echo "service::$SKIJOBS_SERVICE"
echo "skiblobs::$SKIBLOBS"
echo "github_token::$GITHUB_TOKEN"
echo "Building container::$container"

#
# Build container
#
docker build -t $container --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    --build-arg VERSION=$VERSION \
    --force-rm \
    -f ./SlalomTracker.WebApi/Dockerfile . 
#
# To just use the debug image add:
#     --target build \
#

#
# Launch debug container... not logging level overridden below to "Info"
#
docker run --rm --name ski-web -p 5000:5000 -it \
    -e PORT=5000 \
    -e SKIBLOBS="$SKIBLOBS" \
    -e SKISB="$SKISB" \
    -e SKISIGNALR="$SKISIGNALR" \
    -e SKIJOBS_SERVICE="$SKIJOBS_SERVICE" \
    -e Logging__LogLevel__Default="Debug" \
    $container 
    
# dotnet run --project SlalomTracker.WebApi/SlalomTracker.WebApi.csproj

#
# Tag and push the container.
#
docker tag $container wthacr.azurecr.io/$container
docker push wthacr.azurecr.io/$container