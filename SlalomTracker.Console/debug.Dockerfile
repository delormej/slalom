FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build

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
RUN dotnet publish /ski/SlalomTracker.Console/SkiConsole.csproj -c Debug --self-contained true -r linux-x64 -o /ski/bin  && \
    chmod +x /ski/bin/gpmfdemo
