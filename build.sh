# Builds the base docker image used for all builds & debug.
REGISTRY=wthacr
BUILDER_VERSION=1.0

docker build --force-rm --no-cache -t $REGISTRY.azurecr.io/skibuild:$BUILDER_VERSION \
 - < skibuild.Dockerfile  

az acr login -n $REGISTRY

docker push $REGISTRY.azurecr.io/skibuild:$BUILDER_VERSION
