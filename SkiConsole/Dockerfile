FROM microsoft/dotnet:2.1-sdk AS BUILD

RUN apt update && \
    apt install ffmpeg -y

COPY ./ /ski
WORKDIR /ski/SkiConsole
RUN dotnet publish /ski/SkiConsole/SkiConsole.csproj --self-contained true -r linux-x64 -o /ski/bin

#FROM alpine

#ARG ski_blobs_connection
# Environment Variables
#ENV SKIBLOBS=${ski_blobs_connection}
#RUN apt update && \
#    apt install ffmpeg -y

