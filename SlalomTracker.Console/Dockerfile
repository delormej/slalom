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
WORKDIR /ski/bin

# Build and publish skiconsole, ensure that the metadata extractor (gpmfdemo) has execute permissions
# Set to build a self-contained linux binary (no dotnet required).
# use -r linux-musl-x64 to target alpine
# if using alpine, we need to recompile gpmfdemo
RUN dotnet publish /ski/SlalomTracker.Console/SkiConsole.csproj -c Release --self-contained true -r linux-x64 -o /ski/bin  && \
    chmod +x /ski/bin/gpmfdemo

# Alpline looks like smallest, but gpmfdemo needs to be rebuilt for it
#apk add build-base
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.0 AS runtime
# Opt out of globalization: https://github.com/dotnet/core/blob/master/Documentation/self-contained-linux-apps.md
#ENV CORECLR_GLOBAL_INVARIANT=1

# Install Video Processing Libraries 
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
        ffmpeg \
     && rm -rf /var/lib/apt/lists/*

COPY --from=build /ski/bin .