using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AwsSns
{
    class Program
    {
        static async Task Main(string[] args)
        {

            // PART 1 - Publishing team / application
            // ----------------------------------------------------------

            // Create a new SNS client.  The SNS client allows us to interact with and 
            // manage SNS topics. SNS topics are endpoints within the AWS infrastructure that messages
            // can be published too. Topics can be subscribed to by either queues, http endpoints, SMS messages etc etc
            // https://aws.amazon.com/sns/faqs/
            var snsClient = new AmazonSimpleNotificationServiceClient();

            // First thing we are going to do is create a topic.  Topics have a name, this names forms part of the url 
            // of the topics endpoint.  In a standard pub-sub model, we usually set the name fo thr topic to the name / type
            // of the message that will be sent to it.  This allows clients to subscribe to only messages they are interested in.
            // In this example we are going to publish a message called SampleMessage
            var topicRequest = new CreateTopicRequest
            {
                Name = nameof(SampleMessage)
            };
            var createTopicResponse = await snsClient.CreateTopicAsync(topicRequest);

            // At this point the topic has been created.  In a traditional pub sub model, this is where responsibility of the publishing application ends.
            // The only responsibility the publisher has, is to send messages to this topic.  It is usually down to the subscribing application / team
            // to setup subscriptions to this topic.

            // PART 2 - Subscribing team / application
            // ----------------------------------------------------------

            // Once the topic has been created, we could  send messages to this topic, however there are
            // no subscriptions so the messages would never be handled.  What we'll do next is create a SQS queue, then subscribe that queue
            // to the topic therefore any messages published to the topic will end up in the SQS queue.

            // Firstly we need an SQS client to create / manage SQS queues
            var sqsClient = new AmazonSQSClient();

            // Next we create our queue
            var createQueueRequest = new CreateQueueRequest
            {
                QueueName = nameof(SampleMessage)
            };
            var createQueueResponse = await sqsClient.CreateQueueAsync(createQueueRequest);

            // Our SQS queue has now been created however there is no subscription to the topic.  The next part is subscribing our newly created queue
            // to our topic we created earlier.
            var subscriptionArn = await snsClient.SubscribeQueueAsync(createTopicResponse.TopicArn, sqsClient, createQueueResponse.QueueUrl);

            // We have now subscribed our SQS Queue to our topic.  Any messages that are published to our topic will now be delivered to our queue.
            // Lets try this out by sending a message to the topic, then receiving messages from the queue.

            // Lets create an instance of our message class with a unique id
            var sampleMessageSent = new SampleMessage
            {
                Id = Guid.NewGuid()
            };

            // Create a PublishRequest with the message as the body and set the TargetArn to the topic we created earlier
            var publishRequest = new PublishRequest
            {
                TargetArn = createTopicResponse.TopicArn,
                Message = JsonConvert.SerializeObject(sampleMessageSent)
            };

            // Finally publish our message
            Console.WriteLine($"Publishing message with id {sampleMessageSent.Id} to topic {createTopicResponse.TopicArn}");

            await snsClient.PublishAsync(publishRequest);


            // Now we try to receive our message from the queue, not the topic.  
            var receiveResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = createQueueResponse.QueueUrl,
                WaitTimeSeconds = 20
            });

            // Now, each message delivered through SNS is decorated with SNS properties so when a message is 
            // is delivered to a queue, the message itself is not the original message, but a message with SNS specific headers
            // and a property called Message which contains the original message posted to the topic.
            // To make things more complicated, each message that ends up in an SQS queue also has it's own headers etc so we need to do a bit of 
            // formatting before we can get our original published message....

            foreach (var sqsMessage in receiveResponse.Messages)
            {
                var messageWithSnsHeaders = JsonConvert.DeserializeObject<JObject>(sqsMessage.Body);
                var actualMessageJson = messageWithSnsHeaders["Message"].Value<string>();
                var sampleMessage = JsonConvert.DeserializeObject<SampleMessage>(actualMessageJson);
                Console.WriteLine($"Received message {sampleMessage.Id} from queue {createQueueResponse.QueueUrl} via subscription {subscriptionArn}");
                // Note: when we receive a message from SQS, we have to tell SQS that message has been processed successfully otherwise it
                // will re-appear after visibility timeout.  This is SQS's way of ensuring reliability of delivery i.e. it will continue to try to
                // deliver the message until the client application has notified SQS said message has been delivered.
                await sqsClient.DeleteMessageAsync(new DeleteMessageRequest(createQueueResponse.QueueUrl, sqsMessage.ReceiptHandle));
            }

            // In summary, SQS & SNS provide a hugely scalable message bus albeit with some limitations.  Multiple subscribers can subscribe to topics in a variety
            // of ways as well as add filtering etc.  
        }
    }

    public class SampleMessage
    {
        public Guid Id { get; set; }
    }
}
