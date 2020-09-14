# Publish storage events to pub/sub topic.
TOPIC_NAME=video-uploads-topic
BUCKET_NAME=gke-ski-video-uploads
SUBSCRIPTION_ID=video-uploads-subscription

gsutil notification create -t $TOPIC_NAME -f json -e OBJECT_FINALIZE gs://$BUCKET_NAME 

gcloud beta pubsub subscriptions create $SUBSCRIPTION_ID --topic=$TOPIC_NAME
