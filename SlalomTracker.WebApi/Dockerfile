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

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
# RUN apt-get update \
#     &&  apt-get install -y libgdiplus \
#     &&  apt-get install -y --no-install-recommends libc6-dev

RUN wget -q -O /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub && \
    wget https://github.com/sgerrand/alpine-pkg-glibc/releases/download/2.33-r0/glibc-2.33-r0.apk && \
    apk add glibc-2.33-r0.apk

# RUN apk update && apk add libc6-compat

WORKDIR /ski
COPY --from=build /ski/build .

ENTRYPOINT ["dotnet", "SlalomTracker.WebApi.dll"]
