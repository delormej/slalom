FROM mcr.microsoft.com/dotnet/core/sdk:3.0 as build

COPY ./ /ski
WORKDIR /ski

# Publish application
RUN dotnet publish SlalomTracker.SkiJobs.csproj -c Release -o /ski/build/

# create the runtime image (smaller, doesn't need the full sdk for building)
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 as runtime

# Environment Variables
ENV ASPNETCORE_URLS=http://0.0.0.0:80

COPY --from=build /ski/build ./ski
WORKDIR /ski

ENTRYPOINT ["dotnet", "SlalomTracker.SkiJobs.dll"]
