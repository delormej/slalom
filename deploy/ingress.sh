apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  annotations:
    kubernetes.io/ingress.global-static-ip-name: jasondel-pip
    networking.gke.io/managed-certificates: jasondel-certificate
  name: ski-ingress-preview
  namespace: ski
spec:
  rules:
  - host: skipreview.jasondel.com
    http:
      paths:
      - backend:
          serviceName: skiweb-service-preview
          servicePort: 3000
        path: /*
      - backend:
          serviceName: skiwebapi-service-preview
          servicePort: 8080
        path: /api/*
  - host: ski.jasondel.com
    http:
      paths:
      - backend:
          serviceName: skiweb-service
          servicePort: 3000
        path: /*
      - backend:
          serviceName: skiwebapi-service
          servicePort: 8080
        path: /api/*        