ARG  REGISTRY
FROM ${REGISTRY}/skibuild:2.0 AS build

ARG GITHUB_TOKEN
ARG VERSION

WORKDIR /ski
COPY ./ .

# Publish application
RUN dotnet clean ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj && \
    dotnet restore ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj --configfile ./NuGet.Config && \
    dotnet publish ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj -p:Version=$VERSION -c Release -o /ski/build/

# NOTE: Build this docker file with relative path, e.g.:
#   ./SlalomTracker.WebApi/ $ docker build -f Dockerfile ../
# this will allow dotnet build to pull in required relative pathed projects.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine as runtime

# Environment Variables
#ENV ASPNETCORE_URLS=http://0.0.0.0:80

#Adding hack to resolve issue grpc/grpc#21446
RUN echo 'http://dl-cdn.alpinelinux.org/alpine/v3.8/main' >> /etc/apk/repositories && apk update --no-cache && apk add --no-cache bash libc6-compat=1.1.19-r11

WORKDIR /ski
COPY --from=build /ski/build .

ENTRYPOINT ["dotnet", "SlalomTracker.WebApi.dll"]
