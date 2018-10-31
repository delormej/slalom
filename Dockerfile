# dotnet build
FROM microsoft/dotnet:2.1-sdk as build
COPY ./ ./ski
WORKDIR /ski
RUN dotnet publish ./SlalomTracker.WebApi/SlalomTracker.WebApi.csproj -o /ski/build/

# create the runtime image (smaller, doesn't need the full sdk for building)
FROM microsoft/dotnet:2.1-aspnetcore-runtime as runtime
COPY --from=build /ski/build ./ski
WORKDIR /ski

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev

ARG ski_blobs_connection

# Environment Variables
ENV SKIBLOBS=${ski_blobs_connection}
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

# EXPOSE does not appear to work, you still need to run container as such:
# docker run -it -p 5000:5000 skiconsole
#EXPOSE 5000

ENTRYPOINT ["dotnet", "SlalomTracker.WebApi.dll"]
