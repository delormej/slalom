FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build

ARG GITHUB_TOKEN

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev && \
    apt install -y curl && \
    apt install -y procps && \
    apt install unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

COPY ./ /ski
WORKDIR /ski

# Publish application
RUN dotnet restore ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj --configfile ./NuGet.Config && \
    dotnet publish ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj -c Debug -o /ski/build/

# Environment Variables
ENV ASPNETCORE_URLS=http://0.0.0.0:80

#ENTRYPOINT ["dotnet", "/ski/build/SlalomTracker.WebApi.dll"]

# # NOTE: Build this docker file with relative path, e.g.:
# #   ./SlalomTracker.WebApi/ $ docker build -f Dockerfile ../
# # this will allow dotnet build to pull in required relative pathed projects.