FROM microsoft/dotnet:2.1-sdk as build

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev

COPY ./ /ski
WORKDIR /ski

# Force a restore of dependent library which inturn builds gpmf dependency
# Normal build doesn't accomodate this.
RUN dotnet restore ./SlalomTracker/SlalomTracker.csproj
# Publish application
RUN dotnet publish ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj -c Release -o /ski/build/

# create the runtime image (smaller, doesn't need the full sdk for building)
FROM microsoft/dotnet:2.1-aspnetcore-runtime as runtime
#FROM dotnet-debug as runtime

# Environment Variables
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