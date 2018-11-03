docker build -t ski-webapi --build-arg ski_blobs_connection="$SKIBLOBS" -f Dockerfile ../ 
docker run -it -p 80:80 ski-webapi:latest 
