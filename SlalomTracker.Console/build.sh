#
# This file used for DEBUG build
#
rm -rf ref
mkdir -p ref/ 
cp -r ../SlalomTracker/bin/Debug/* ./ref/
docker build -t skiconsole -f debug.Dockerfile .
#
# Launch debug container
#
# Need to input env variable value here:
#SKIBLOBS=
docker run -it --rm -e SKIBLOBS=\"$SKIBLOBS\" --name ski-dbg skiconsole /bin/bash
