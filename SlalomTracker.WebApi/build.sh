#!/bin/bash
#source prebuild.sh

# CI/CD could override this version.
if [ -z "$VERSION" ]
then 
    VERSION=2.2.0
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
    -f ./SlalomTracker.WebApi/Dockerfile . 
#
# To just use the debug image add:
#     --target build \
#

#
# Launch debug container... not logging level overridden below to "Info"
#
docker run --rm --name ski-dbg -p 80:80 -it \
    -e SKIBLOBS='$SKIBLOBS' \
    -e SKIJOBS_SERVICE="$SKIJOBS_SERVICE" \
    -e Logging__LogLevel__Default="Debug" \
    $container 
    
#    dotnet -- run --project SlalomTracker.WebApi/SlalomTracker.WebApi.csproj

#
# Tag and push the container.
#
docker tag $container wthacr.azurecr.io/$container
docker push wthacr.azurecr.io/$container