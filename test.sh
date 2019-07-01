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

 