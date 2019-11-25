export rg=ski-aks
export aks_name=ski-aks
export location=eastus
export kversion=1.14.8
export acr=jasondelAcr

# Get the name of VM SKU:
# az vm list-skus -l eastus2 -o table 
export node_size=Standard_B2s
export node_count=2

# Ensure that your susbscription is registered to use ACI
# az provider register --namespace Microsoft.ContainerInstance

# create a group to hold all resources
az group create --name $rg --location $location --tags owner=jasondel

export appId=887f156d-8fd8-43e8-b6ee-ec15c02e37ed
export password=$(az keyvault secret show --name aks-sp-secret --vault-name delormejKV --query value -o tsv)

acrId=$(az acr list -o tsv --query "[?name=='$acr'].id")

az aks create \
    --resource-group $rg \
    --name $aks_name \
    --network-plugin azure \
    --service-principal $appId \
    --client-secret "$password" \
    --kubernetes-version $kversion \
    --node-vm-size $node_size \
    --generate-ssh-keys \
    --node-count $node_count \
    --enable-cluster-autoscaler \
    --min-count 1 \
    --max-count $(expr $node_count + 2) \
    --attach-acr $acrId 

# Grab the credentials.
az aks get-credentials --overwrite-existing -g $rg -n $aks_name

