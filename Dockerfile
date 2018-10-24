# dotnet build
FROM microsoft/dotnet:2.1-sdk as build
COPY ./ ./ski
WORKDIR /ski
RUN dotnet publish ./SkiConsole/SkiConsole.csproj -o /ski/build/

FROM microsoft/dotnet:2.1-runtime as runtime
COPY --from=build /ski/build ./ski
WORKDIR /ski
# copy the gpmf output here as well
#COPY ./build/gpmfdemo ./ski/

#ENTRYPOINT [ "dotnet", "ski.dll" ]