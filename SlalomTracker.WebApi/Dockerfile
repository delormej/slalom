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

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim as runtime

# Environment Variables
#ENV ASPNETCORE_URLS=http://0.0.0.0:80

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus

WORKDIR /ski
COPY --from=build /ski/build .

ENTRYPOINT ["dotnet", "SlalomTracker.WebApi.dll"]
