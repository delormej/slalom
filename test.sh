#cat list.txt | jq  '.[]."videoUrl"'
#curl -X POST -d "" "http://ski-app.azurewebsites.net/api/image?videoUrl=http://skivideostorage.blob.core.windows.net/ski/2018-06-08/GOPR0188.MP4" 
while read p; do \
  echo "$p"
done <list2.txt


cat list.txt | jq  '.[]."videoUrl"' | while read p; do \ 
  curl -X POST -d "" "http://ski-app.azurewebsites.net/api/image?videoUrl=$p"

cat list.txt | jq  '.[]."videoUrl"' | while read p; do \ 
    echo `"$p"`
done


while IFS="" read -r p || [ -n "$p" ]
do
  printf '%ncurl -X POST -d "" "http://ski-app.azurewebsites.net/api/image?videoUrl=$p"' 
done < list2.txt

 cat list.txt | jq  '.[]."videoUrl"' -r | while read line ; do curl -X POST -d "" "http://ski-app.azurewebsites.net/api/image?videoUrl=$line" ; done

 curl -X POST -d "{'Url': 'http://skivideostorage.blob.core.windows.net/ski-ingest/2019-05-17/011.MP4'}" http://ski-app.azurewebsites.net/api/processvideo

# Test the update service:
 curl -v -i -d '{"url":"https://skivideostorage.blob.core.windows.net/ski/2019-07-11/GOPR1300_ts.MP4","thumbnailUrl":"https://skivideostorage.blob.core.windows.net/ski/2019-07-11/GOPR1300.PNG","jsonUrl":"https://skivideostorage.blob.core.windows.net/ski/2019-07-11/GOPR1300_ts.json","skier":"Jason","ropeLengthM":0,"boatSpeedMph":32.3,"hasCrash":false,"all6Balls":false,"courseName":"outside","entryTime":5.227,"slalomTrackerVersion":"SlalomTracker:v1.1.0.0\nSlalomTracker.WebApi:v6.3.0.0","partitionKey":"2019-07-11","rowKey":"GOPR1300_ts.MP4","timestamp":"2019-07-17T12:18:10.8103607+00:00","eTag":"W/\"datetime'2019-07-11T12%3A18%3A10.8103607Z'\""}' -H "Content-Type: application/json"  -X POST http://localhost/api/updatevideo

# Clean up aci jobs:
curl -X POST -d "" http://ski-app.azurewebsites.net/api/acicleanup

 # List containers
 az container list --query '[].[name,provisioningState,containers[0].command[2]]' -o table

 # Get container logs
 az container logs -g ski-jobs -n aci-7c93df05

 # Trim a video
 # 1) Launch container (on Windows), mounting the directory with the video.
 docker run -it --entrypoint bash -v "d:/Users/delormej/dev/Video:/video" jrottenberg/ffmpeg
 # 2) Launch ffmpeg (these could be combined)
 ffmpeg -i /video/GOPR1362.MP4 -ss 00:00:58 -t 00:00:50 -map 0:v -map 0:a -map 0:3 -copy_unknown -tag:2 gpmd -c copy /video/GOPR1362-b.MP4

curl -X POST -d "{'Url': 'https://skivideostorage.blob.core.windows.net/dev-ski-ingest/GOPR2175.MP4'}" http://dev-ski-app.azurewebsites.net/api/processvideo
curl -X POST -d "{'Url': 'https://skivideostorage.blob.core.windows.net/ski-ingest/GOPR1908.MP4'}" http://dev-ski-app.azurewebsites.net/api/processvideo
curl -X POST -d "{'Url': 'https://skivideostorage.blob.core.windows.net/dev-ski-ingest/GOPR2304.MP4'}" http://dev-ski-app.azurewebsites.net/api/processvideo
curl -X POST -d "{'Url': 'https://skivideostorage.blob.core.windows.net/dev-ski-ingest/GOPR2181.MP4'}" http://dev-ski-app.azurewebsites.net/api/processvideo
curl -X POST -d "{'Url': 'https://skivideostorage.blob.core.windows.net/dev-ski-ingest/GOPR1894.MP4'}" http://dev-ski-app.azurewebsites.net/api/processvideo
  