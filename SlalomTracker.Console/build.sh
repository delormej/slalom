#!/bin/bash
#source prebuild.sh

# CI/CD could override this version.
if [ -z "$VERSION" ]
then 
    VERSION=5.0.2
fi
container=skiconsole:v$VERSION

echo "service::$SKIJOBS_SERVICE"
echo "skiblobs::$SKIBLOBS"
echo "github_token::$GITHUB_TOKEN"
echo "Building container::$container"

#
# Build container
#
echo "Building container."
docker build -t $container --build-arg GITHUB_TOKEN=$GITHUB_TOKEN \
    --build-arg VERSION=$VERSION \
    -f ./SlalomTracker.Console/Dockerfile .
#
# To just use the debug image add --target build to the above and it won't build the release stage.
#

#
# Launch container
#
# env variable skiblobs still not working...
docker run -it --rm \
    -v $PWD:/shared \
    -e SKIBLOBS='$SKIBLOBS' \
    -e SKISB='$SKISB' \
    -e GOOGLE_APPLICATION_CREDENTIALS="/ski/gcloud.json" \
    --name ski-dbg \
    $container

docker tag $container wthacr.azurecr.io/$container
docker push wthacr.azurecr.io/$container