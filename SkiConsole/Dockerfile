# dotnet build
FROM microsoft/dotnet:2.1-sdk as build
COPY ./ ./ski
WORKDIR /ski
RUN dotnet publish ./SkiConsole/SkiConsole.csproj -o ./build/

FROM microsoft/dotnet:2.1-runtime as runtime
COPY --from=build /skiconsole/build/ .
# copy the gpmf output here as well
COPY ./build/gpmfdemo .
