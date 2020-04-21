FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ARG GITHUB_TOKEN
ARG VERSION

# Install Video Processing Libraries in FFMPEG
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
        ffmpeg \
        curl \
        procps \
        unzip \
     && rm -rf /var/lib/apt/lists/* \
     && curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg     

COPY ./ /ski
WORKDIR /ski/bin

# Build and publish skiconsole, ensure that the metadata extractor (gpmfdemo) has execute permissions
# Set to build a self-contained linux binary (no dotnet required).
# use -r linux-musl-x64 to target alpine
# if using alpine, we need to recompile gpmfdemo
RUN dotnet clean /ski/SlalomTracker.Console/SkiConsole.csproj && \
    dotnet restore /ski/SlalomTracker.Console/SkiConsole.csproj --configfile /ski/NuGet.Config && \
    dotnet publish /ski/SlalomTracker.Console/SkiConsole.csproj  -p:Version=$VERSION -c Release --self-contained true -r linux-x64 -o /ski/bin  && \
    chmod +x /ski/bin/gpmfdemo

# Alpline looks like smallest, but gpmfdemo needs to be rebuilt for it
#apk add build-base
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1 AS runtime
# Opt out of globalization: https://github.com/dotnet/core/blob/master/Documentation/self-contained-linux-apps.md
#ENV CORECLR_GLOBAL_INVARIANT=1

WORKDIR /ski

# Install Video Processing Libraries 
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
        ffmpeg \
     && rm -rf /var/lib/apt/lists/*

COPY --from=build /ski/bin /ski