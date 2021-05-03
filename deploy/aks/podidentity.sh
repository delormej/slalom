#!/bin/bash
rg=ski-aks
name=ski-aks
identity_name=skijobs-podid
skijobs_rg=ski-jobs

mc_rg=$(az aks show -n $name -g $rg --query nodeResourceGroup -o tsv)
echo "mc_rg=$mc_rg"

export aadid=$(az identity create -g $mc_rg -n $identity_name -o tsv --query clientId)
echo "aaid=$aadid"

export aad_resourceid=$(az identity show -g $mc_rg -n $identity_name -o tsv --query id)
echo "aad_resourceid=$aad_resourceid"

spid=$(az aks show -g $rg -n $name --query servicePrincipalProfile.clientId -o tsv)
echo "spid=$spid"

# Required for pod identity to work.
az role assignment create --role "Managed Identity Operator" --assignee $spid --scope $aadid

# Give this identity full access to the ski-jobs resource group
skijobs_rgid=$(az group show --name $skijobs_rg --query id -o tsv)
echo "skijobs_rgid=$skijobs_rgid"

az role assignment create --role "Contributor" --assignee $aadid --scope $skijobs_rgid
