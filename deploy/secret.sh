#! bin/bash
# This one actually needs to be done by hand... make sure there are no extra chars in the env variable.
kubectl create secret generic skisb-secret --from-literal=connectionstring="$SKISB" -n ski
kubectl create secret generic skiblob-secret --from-literal=connectionstring="$SKIBLOBS" -n ski
kubectl create secret generic skisignalr-secret --from-literal=connectionstring="$SKISIGNALR" -n ski
kubectl create secret generic skiml-secret --from-literal=key="$SKIMLKEY" -n ski
cp ../gcloud.json ../key.json
kubectl create secret generic gcloud-key --from-file ../key.json -n ski

# For non-Azure clusters use this to create the docker password for ACR.
kubectl create secret docker-registry acr-secret \
    --docker-server="wthacr.azurecr.io" --docker-username="wthAcr" \
    --docker-password="$ACRPASSWORD" \
    --docker-email="wthacr@jasondel.com" 
#
# Now associate secret with service account so you don't have to manually specify each time.
#
kubectl patch serviceaccount default -p '{"imagePullSecrets": [{"name": "wthacr"}]}'

#
# Install certbot-auto with these instructions: https://certbot.eff.org/docs/using.html?highlight=renew#manual
# Then follow these "--manual" instructions to create a cert with dns challenge:
# https://certbot.eff.org/docs/using.html?highlight=renew#manual
#
sudo kubectl create secret tls jasondel-com-tls --key="/etc/letsencrypt/live/jasondel.com/privkey.pem" --cert="/etc/letsencrypt/live/jasondel.com/cert.pem"

# How to get json file for GCP from a previously stored secret:
#kubectl get secret -n ski gcloud-key -o jsonpath='{.data.key\.json}' | base64 -d > key.json