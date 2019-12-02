# Install HELM
curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash  

# Add a default repo.
helm repo add stable https://kubernetes-charts.storage.googleapis.com

# Install ingress
helm install ski-ingress stable/nginx-ingress

# To delete:
# helm delete ski-ingress