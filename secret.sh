#! bin/bash
# This one actually needs to be done by hand... make sure there are no extra chars in the env variable.
kubectl create secret generic skisb-secret --from-literal=connectionstring="xxxxx" -n ski
kubectl create secret generic skiblob-secret --from-literal=connectionstring="xxxxx" -n ski
kubectl create secret generic skisignalr-secret --from-literal=connectionstring="xxxx" -n ski