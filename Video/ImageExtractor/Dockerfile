FROM microsoft/dotnet-framework:4.6.2
# Remote debug ports
EXPOSE 4020 4021

# Install VS2015 C++ Runtime required for Accord.Video.FFMPEG (both x86 & x64 required)
RUN PowerShell Invoke-WebRequest -OutFile c:\vc_redist.x86.exe -Uri https://download.microsoft.com/download/6/A/A/6AA4EDFF-645B-48C5-81CC-ED5963AEAD48/vc_redist.x86.exe
RUN PowerShell Invoke-WebRequest -OutFile c:\vc_redist.x64.exe -Uri https://download.microsoft.com/download/6/A/A/6AA4EDFF-645B-48C5-81CC-ED5963AEAD48/vc_redist.x64.exe
RUN c:\vc_redist.x86.exe /install /passive /norestart
RUN c:\vc_redist.x64.exe /install /passive /norestart

# Install remote debug tools
RUN PowerShell Invoke-WebRequest -OutFile c:\rtools_setup_x64.exe -Uri https://aka.ms/vs/15/release/RemoteTools.amd64ret.enu.exe 
RUN c:\rtools_setup_x64.exe /install /quiet

COPY ./bin/net462/ /ImageExtractor/

####################################################
# Steps to remote debug from VS into the container
####################################################
# Magic to figure out how to remote debug is documented here: https://www.richard-banks.org/2017/02/debug-net-in-windows-container.html
#docker run -it -v c:\users\delor\dev\slalom\video:c:\video -p 4020:4020 -p 4021:4021 ski-imageextractor:v2
#docker exec -it <container-id> "C:\Program Files\Microsoft Visual Studio 15.0\Common7\IDE\Remote Debugger\x64\msvsmon.exe" /nostatus /silent /noauth /anyuser /nosecuritywarn

####################################################
# Steps to push to ACR & run in ACI:
####################################################
#
# 1. Tag the container with registry prefix:
#       docker tag ski-imageextractor:v8 jasondelacr.azurecr.io/ski-imageextractor:v8
#
# 2. login to the registry
#       docker login -u jasondelAcr -p <removed> jasondelacr.azurecr.io
#
# 3. push the image to ACR:
#       docker push jasondelacr.azurecr.io/ski-imageextractor:v8
#
# 4. launch in Azure Container Instance:
#       az container create -g ski -n ski-imageextractor --image jasondelacr.azurecr.io/ski-imageextractor:v8 --os-type Windows --restart-policy OnFailure --registry-username jasondelAcr --registry-password <removed>
#
# 5. test! this should bring up a command prompt:
#       az container exec -g ski -n ski-imageextractor --exec-command "cmd.exe"
#
