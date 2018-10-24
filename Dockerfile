# dotnet build
FROM microsoft/dotnet:2.1-sdk as build
COPY ./ ./ski
WORKDIR /ski
RUN dotnet publish ./SkiConsole/SkiConsole.csproj -o /ski/build/

# create the runtime image (smaller, doesn't need the full sdk for building)
FROM microsoft/dotnet:2.1-runtime as runtime
COPY --from=build /ski/build ./ski
WORKDIR /ski

# Workaround, dependencies for graphics libraries, per this issue: https://github.com/dotnet/corefx/issues/25102
RUN apt-get update \
    &&  apt-get install -y libgdiplus \
    &&  apt-get install -y --no-install-recommends libc6-dev

#ENTRYPOINT [ "dotnet", "ski.dll" ]