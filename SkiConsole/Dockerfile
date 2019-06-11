FROM microsoft/dotnet:2.1-sdk AS BUILD

# Install Video Processing Libraries in FFMPEG
RUN apt update && \
    apt install ffmpeg -y

COPY ./ /ski
WORKDIR /ski/bin
# Build and publish skiconsole, ensure that the metadata extractor (gpmfdemo) has execute permissions
# Set to build a self-contained linux binary (no dotnet required).
RUN dotnet publish /ski/SkiConsole/SkiConsole.csproj --self-contained true -r linux-x64 -o /ski/bin && \
    chmod +x gpmfdemo

#FROM alpine

#ARG ski_blobs_connection
# Environment Variables
#ENV SKIBLOBS=${ski_blobs_connection}
#RUN apt update && \
#    apt install ffmpeg -y

