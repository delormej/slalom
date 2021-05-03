#!/bin/bash
#
# Deploys the application to the currently connected K8S cluster.
# Assumes that you have az cli, k8s, helm3 installed
# Assumes you are logged into az subscription and connected to AKS cluster
#

# Load configuration values.
vaultName=delormejKV
export BUILDNUMBER=$(git describe --abbrev=0 --tags)
export SKIBLOBS=$(az keyvault secret show --name SKIBLOBS --vault-name $vaultName --query value -o tsv)
export FacebookSecret=$(az keyvault secret show --name FacebookSecret --vault-name $vaultName --query value -o tsv)
export GOOGLESKIVIDEOS=$(az keyvault secret show --name GOOGLESKIVIDEOS --vault-name $vaultName --query value -o tsv)
export SUBSCRIPTIONID=$(az account show --query id -o tsv)


# deploy ingress
#./ingress.sh

export PUBLICIPID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$IP')].[id]" --output tsv)

# helm install ski-$BUILDNUMBER \
#     --set buildNumber=$BUILDNUMBER \
#     --set publicIp=PUBLICIP 

