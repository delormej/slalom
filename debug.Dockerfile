FROM microsoft/dotnet:2.1-sdk as dotnet-debug
RUN apt update && \
    apt install -y curl && \
    apt install -y procps && \
    apt install unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

