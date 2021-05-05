# Builds the base docker image used for all builds & debug.
REGISTRY=gcr.io/$GOOGLE_PROJECT_ID
BUILDER_VERSION=2.0

docker build --force-rm --no-cache -t $REGISTRY/skibuild:$BUILDER_VERSION \
 - < skibuild.Dockerfile  

docker push $REGISTRY/skibuild:$BUILDER_VERSION
