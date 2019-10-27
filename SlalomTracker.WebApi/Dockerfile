FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build

# Install Video Processing Libraries in FFMPEG
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
        ffmpeg \
     && rm -rf /var/lib/apt/lists/*

COPY ./ /ski
WORKDIR /ski

# Publish application
RUN dotnet publish ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj -c Release -o /ski/build/

# create the runtime image (smaller, doesn't need the full sdk for building)
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 as runtime
#FROM dotnet-debug as runtime

# Environment Variables
ENV ASPNETCORE_URLS=http://0.0.0.0:80

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
        ffmpeg \
     && rm -rf /var/lib/apt/lists/*

COPY --from=build /ski/build ./ski
WORKDIR /ski

ENTRYPOINT ["dotnet", "SlalomTracker.WebApi.dll"]

# NOTE: Build this docker file with relative path, e.g.:
#   ./SlalomTracker.WebApi/ $ docker build -f Dockerfile ../
# This will allow dotnet build to pull in required relative pathed projects.