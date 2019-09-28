FROM microsoft/dotnet:2.1-sdk AS build
#FROM dotnet-debug
ARG nuget

# Install Video Processing Libraries in FFMPEG
RUN apt update && \
    apt install ffmpeg -y

# Make the ref dir regardless of whether we copy locally into it or not or nuget will fail on restore attempt.
RUN mkdir -p /ski/ref
COPY ./ /ski
WORKDIR /ski/bin

# Install nuget credential provider, to pull dependencies from Azure Artifacts feed.  This feed is specified in Nuget.config.  
RUN curl -s https://raw.githubusercontent.com/microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | bash
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS=$nuget

# Build and publish skiconsole, ensure that the metadata extractor (gpmfdemo) has execute permissions
# Set to build a self-contained linux binary (no dotnet required).
# use -r linux-musl-x64 to target alpine
# if using alpine, we need to recompile gpmfdemo
RUN dotnet publish /ski/SkiConsole.csproj -c Release --self-contained true -r linux-x64 -o /ski/bin  && \
    chmod +x /ski/bin/gpmfdemo

# Copy debug libraries
#COPY ./ref/netstandard2.0/*.pdb ./bin/

# Alpline looks like smallest, but gpmfdemo needs to be rebuilt for it
#apk add build-base
#FROM debian AS runtime
# Opt out of globalization: https://github.com/dotnet/core/blob/master/Documentation/self-contained-linux-apps.md
#ENV CORECLR_GLOBAL_INVARIANT=1
# Install Video Processing Libraries 
#RUN apt-get update && \
#    apt-get install ffmpeg -y

#COPY --from=build /ski/bin .