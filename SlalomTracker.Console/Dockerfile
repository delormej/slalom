ARG  REGISTRY
FROM ${REGISTRY}/skibuild:2.0 AS build

ARG GITHUB_TOKEN
ARG VERSION

COPY . /ski
WORKDIR /ski

# Build and publish skiconsole, ensure that the metadata extractor (gpmfdemo) has execute permissions
# Set to build a self-contained linux binary (no dotnet required).
# use -r linux-musl-x64 to target alpine
# if using alpine, we need to recompile gpmfdemo
RUN if [ -d "./SlalomTracker/gpmf/build/" ]; then rm -Rf ./SlalomTracker/gpmf/build/; fi && \
    mkdir ./SlalomTracker/gpmf/build/ && \
    make --directory ./SlalomTracker/gpmf/demo && \
    cp ./SlalomTracker/gpmf/demo/gpmfdemo ./SlalomTracker/gpmf/build/

RUN dotnet clean /ski/SlalomTracker.Console/SkiConsole.csproj && \
    dotnet restore /ski/SlalomTracker.Console/SkiConsole.csproj \
        --configfile /ski/NuGet.Config && \
    dotnet publish /ski/SlalomTracker.Console/SkiConsole.csproj \
        -p:Version=$VERSION -c Release --self-contained true \
        --configfile /ski/NuGet.Config \
        -r linux-x64 -o /ski/bin  \
        -p:PublishTrimmed=True -p:TrimMode=Link \
        /p:PublishSingleFile=true /p:IncludeNativeLibrariesInSingleFile=true

# Alpline looks like smallest, but gpmfdemo needs to be rebuilt for it
#apk add build-base
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0 AS runtime
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