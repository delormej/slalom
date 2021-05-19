GOOGLE_PROJECT_ID=$(gcloud config list --format 'value(core.project)' 2>/dev/null)
PROJECT_NUMBER=gcloud projects describe $GOOGLE_PROJECT_ID --format 'value(projectNumber)' 2>/dev/null

# Publish storage events to pub/sub topic.
BUCKET_NAME=skivideo_upload
TOPIC_NAME=skivideo-upload-topic
SUBSCRIPTION_ID=skivideo-upload-subscription

#
# Create a topic for new objects written to the bucket.
#
gsutil notification create -t $TOPIC_NAME -f json -e OBJECT_FINALIZE gs://$BUCKET_NAME 

gcloud pubsub topics create $TOPIC_NAME-deadletter

gcloud pubsub subscriptions create $SUBSCRIPTION_ID \
  --topic=$TOPIC_NAME \
  --dead-letter-topic=$TOPIC_NAME-deadletter \
  --max-delivery-attempts=5 

#
# Create a subscription for the dead-letter queue.
#
gcloud pubsub subscriptions create $SUBSCRIPTION_ID-deadletter \
  --topic=$TOPIC_NAME-deadletter 

#
# Assign permissions for pubsub to forward to deadletter topic.
#
PROJECT_NUMBER=$(gcloud projects describe --format="value(projectNumber)" $GOOGLE_PROJECT_ID)

PUBSUB_SERVICE_ACCOUNT="service-$PROJECT_NUMBER@gcp-sa-pubsub.iam.gserviceaccount.com"

gcloud pubsub topics add-iam-policy-binding $TOPIC_NAME-deadletter \
    --member="serviceAccount:$PUBSUB_SERVICE_ACCOUNT"\
    --role="roles/pubsub.publisher"

gcloud pubsub subscriptions add-iam-policy-binding $SUBSCRIPTION_ID \
    --member="serviceAccount:$PUBSUB_SERVICE_ACCOUNT"\
    --role="roles/pubsub.subscriber"
