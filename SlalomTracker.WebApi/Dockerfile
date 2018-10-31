FROM microsoft/dotnet:2.1-sdk as build
COPY ./ /slalom
WORKDIR /slalom/SlalomTracker.WebApi
RUN dotnet publish ./SlalomTracker.WebApi.csproj -o ./build/

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev

EXPOSE 5000

ENTRYPOINT ["dotnet", "run"]

# NOTE: Build this docker file with relative path, e.g.:
#   ./SlalomTracker.WebApi/ $ docker build -f Dockerfile ../
# this will allow dotnet build to pull in required relative pathed projects.