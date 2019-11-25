rg=ski-aks
name=ski-aks
identity_name=skijobs-podid
skijobs_rg=ski-jobs

mc_rg=$(az aks show -n $name -g $rg --query nodeResourceGroup -o tsv)
echo "mc_rg=$mc_rg"

aadid=$(az identity create -g $mc_rg -n $identity_name -o tsv --query clientId)
echo "aaid=$aadid"

aad_resourceid=$(az identity show -g $mc_rg -n $identity_name -o tsv --query id)
echo "aad_resourceid=$aad_resourceid"

spid=$(az aks show -g $rg -n $name --query servicePrincipalProfile.clientId -o tsv)
echo "spid=$spid"

# Required for pod identity to work.
az role assignment create --role "Managed Identity Operator" --assignee $spid --scope $aadid

# Give this identity full access to the ski-jobs resource group
skijobs_rgid=$(az group show --name $skijobs_rg --query id -o tsv)
echo "skijobs_rgid=$skijobs_rgid"

az role assignment create --role "Contributor" --assignee $aadid --scope $skijobs_rgid

# Create the K8S object for identity
cat <<EOF >azureidentity.yaml
apiVersion: "aadpodidentity.k8s.io/v1"
kind: AzureIdentity
metadata:
 name: $identity_name
spec:
 type: 0
 ResourceID: $aad_resourceid
 ClientID: $aadid 
EOF

# Create the K8S object to bind identity to pods that match Selector.
cat <<EOF > azureidentitybinding.yaml
apiVersion: "aadpodidentity.k8s.io/v1"
kind: AzureIdentityBinding
metadata:
  name: $identity_name-binding
spec:
  AzureIdentity: $identity_name
  Selector: $identity_name
EOF

# Deploy these to K8S
kubectl apply -f ./azureidentity.yaml
kubectl apply -f ./azureidentitybinding.yaml
  