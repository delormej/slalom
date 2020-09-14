# Publish storage events to pub/sub topic.
TOPIC_NAME=video-uploaded
BUCKET_NAME=upload
SUBSCRIPTION_ID=video-uploaded-sub

gsutil notification create -t $TOPIC_NAME -f json gs://$BUCKET_NAME -e OBJECT_FINALIZE

gcloud beta pubsub subscriptions create $SUBSCRIPTION_ID \
  --topic=$TOPIC_NAME \
  --message-filter='FILTER'
