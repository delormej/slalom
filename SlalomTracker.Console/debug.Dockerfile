FROM microsoft/dotnet:2.1-sdk as dotnet-debug
RUN apt update && \
    apt install -y curl && \
    apt install -y procps && \
    apt install unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

# Install Video Processing Libraries in FFMPEG
RUN apt update && \
    apt install ffmpeg -y

# Make the ref dir regardless of whether we copy locally into it or not or nuget will fail on restore attempt.
RUN mkdir -p /ski/ref
COPY ./ /ski
WORKDIR /ski/bin

# Build and publish skiconsole, ensure that the metadata extractor (gpmfdemo) has execute permissions
# Set to build a self-contained linux binary (no dotnet required).
# use -r linux-musl-x64 to target alpine
# if using alpine, we need to recompile gpmfdemo
RUN dotnet publish /ski/SkiConsole.csproj -c Debug --self-contained true -r linux-x64 -o /ski/bin  && \
    chmod +x /ski/bin/gpmfdemo

# Copy debug symbols 
COPY ../ref/netstandard2.0/*.pdb .