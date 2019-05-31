FROM microsoft/dotnet:2.1-sdk AS installer-env

COPY ./ /ski
WORKDIR /ski/SlalomTracker.OnVideoQueued
RUN mkdir -p /home/site/wwwroot && \
    dotnet build -c Debug ./SlalomTracker.OnVideoQueued.csproj -o /home/site/wwwroot
RUN dotnet publish /ski/SkiConsole/SkiConsole.csproj --self-contained true -r linux-x64 -o /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet:2.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

ARG ski_blobs_connection
# Environment Variables
ENV SKIBLOBS=${ski_blobs_connection}

RUN apt update && \
    apt install -y curl && \
    apt install -y procps && \
    apt install unzip && \
    apt install ffmpeg -y && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
COPY --from=installer-env /ski/SlalomTracker.OnVideoQueued/build/gpmfdemo /azure-functions-host/
COPY --from=installer-env /ski/SkiConsole /azure-functions-host/

# Build with this from the root of the workspace $ slalom/
# docker build -t skivideofunction:debug -f ./SlalomTracker.OnVideoQueued/debug.Dockerfile . 
# Execute debug with:
# It will error out without the appinsights key and skiblobs env variables
# docker run -it -e SKIBLOBS=$SKIBLOBS -e APPINSIGHTS_INSTRUMENTATIONKEY=627db034-95e1-4e6c-b277-46cb1bbb58d8 --name ski-dbg skivideofunction:debug