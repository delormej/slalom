FROM microsoft/dotnet:2.1-sdk AS installer-env

COPY ./ /ski
WORKDIR /ski/SlalomTracker.OnVideoQueued
RUN mkdir -p /home/site/wwwroot && \
    dotnet publish ./SlalomTracker.OnVideoQueued.csproj -o /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet:2.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

ARG ski_blobs_connection
# Environment Variables
ENV SKIBLOBS=${ski_blobs_connection}

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
COPY --from=installer-env /ski/SlalomTracker.OnVideoQueued/build/gpmfdemo /azure-functions-host/
