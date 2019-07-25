FROM microsoft/dotnet:2.1-sdk as build
ARG ski_blobs_connection

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev

COPY ./ /ski
WORKDIR /ski/SlalomTracker.WebApi
ENV SKIBLOBS=${ski_blobs_connection}

# Execute unit test
RUN dotnet test ./SlalomTracker.WebApi.csproj --logger "trx;LogFileName=results.trx"

# Publish application
RUN dotnet publish ./SlalomTracker.WebApi.csproj -o /ski/build/ --source .

# create the runtime image (smaller, doesn't need the full sdk for building)
FROM microsoft/dotnet:2.1-aspnetcore-runtime as runtime
#FROM dotnet-debug as runtime
ARG ski_blobs_connection
# Environment Variables
ENV SKIBLOBS=${ski_blobs_connection}
ENV ASPNETCORE_URLS=http://0.0.0.0:80

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev

COPY --from=build /ski/build ./ski
WORKDIR /ski

ENTRYPOINT ["dotnet", "SlalomTracker.WebApi.dll"]

# # NOTE: Build this docker file with relative path, e.g.:
# #   ./SlalomTracker.WebApi/ $ docker build -f Dockerfile ../
# # this will allow dotnet build to pull in required relative pathed projects.