# snssqsdemo

This simple appliction creates an SNS topic, creates a SQS queue and sets up a subscription from the topic to the queue so that any message published to the topic is forwarded to the queue.  It then publishes a serialized object to the topic and subsequently reads it from the queue.

NB: This application can be run locally, in order to do that you must have configured your aws access credentials appropriately using the command line e.g.

```shell
aws configure
```

You must also have appropriate permissions in aws to create SQS queues, topics and subscriptions.

Sample output:

```shell
Publishing message with id 72ad17d2-9b5a-4c93-b591-6d80f2861b52 to topic arn:aws:sns:us-east-1:276489849189:SampleMessage
Received message 72ad17d2-9b5a-4c93-b591-6d80f2861b52 from queue https://sqs.us-east-1.amazonaws.com/276489849189/SampleMessage via subscription arn:aws:sns:us-east-1:276489849189:SampleMessage:a309be58-a3b9-4dfc-8b08-edc668890b88
```
