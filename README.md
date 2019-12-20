# snssqsdemo

This simple appliction creates an SNS topic, creates a SQS queue and sets up a subscription from the topic to the queue so that any message published to the topic is forwarded to the queue.  It then publishes a message to the topic and subsequently reads it from the queue.

NB: This application can be run locally, in order to do that you must have configured your aws access credentials appropriately using the command line e.g.

```shell
aws configure
```

You must also have appropriate permissions in aws to create SQS queues, topics and subscriptions.
