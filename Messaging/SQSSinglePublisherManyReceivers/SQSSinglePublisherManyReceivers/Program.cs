using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Nito.AsyncEx;

namespace SQSSinglePublisherManyReceivers
{
    class Program
    {
        private static bool _receieverShouldDeleteMessage = false;
        private static AmazonSQSClient _sqs = new AmazonSQSClient();
        private static string _myQueueUrl;


        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }


        static async void MainAsync(string[] args)
        {
            try
            {
                Console.WriteLine("===========================================");
                Console.WriteLine("Getting Started with Amazon SQS");
                Console.WriteLine("===========================================\n");

                //Creating a queue
                Console.WriteLine("Create a queue called MyQueue.\n");
                var sqsRequest = new CreateQueueRequest { QueueName = "MyQueue11" };
                var createQueueResponse = await _sqs.CreateQueueAsync(sqsRequest);
                _myQueueUrl = createQueueResponse.QueueUrl;

                //Confirming the queue exists
                var listQueuesRequest = new ListQueuesRequest();
                var listQueuesResponse = await _sqs.ListQueuesAsync(listQueuesRequest);

                Console.WriteLine("Printing list of Amazon SQS queues.\n");
                if (listQueuesResponse.QueueUrls != null)
                {
                    foreach (String queueUrl in listQueuesResponse.QueueUrls)
                    {
                        Console.WriteLine("  QueueUrl: {0}", queueUrl);
                    }
                }
                Console.WriteLine();

                //Sending a message
                for (int i = 0; i < 10; i++)
                {
                    var message = $"This is my message text-Id-{Guid.NewGuid().ToString("N")}";
                    //var message = $"This is my message text";
                    Console.WriteLine($"Sending a message to MyQueue : {message}");
                    var sendMessageRequest = new SendMessageRequest
                    {
                        QueueUrl = _myQueueUrl, //URL from initial queue creation
                        MessageBody = message
                    };
                    await _sqs.SendMessageAsync(sendMessageRequest);
                }

                //start of 5 receiver tasks
                var tasks = Enumerable.Range(0, 5).Select(number =>
                    Task.Run(async () =>
                        await ReceiveMessage(number)
                    )).ToList();

                await Task.WhenAll(tasks);

            }
            catch (AmazonSQSException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
            }

            Console.WriteLine("Press Enter to continue...");
            Console.Read();
        }


        private static async Task ReceiveMessage(int state)
        {
            //Receiving a message
            var receiveMessageRequest = new ReceiveMessageRequest { QueueUrl = _myQueueUrl };
            var receiveMessageResponse = await _sqs.ReceiveMessageAsync(receiveMessageRequest);
            if (receiveMessageResponse.Messages != null)
            {
                Console.WriteLine($"Receiever {state} Printing received message.\n");
                foreach (var message in receiveMessageResponse.Messages)
                {
                    Console.WriteLine($"Receiever {state}   Message");
                    if (!string.IsNullOrEmpty(message.MessageId))
                    {
                        Console.WriteLine($"Receiever {state}     MessageId: {message.MessageId}");
                    }
                    if (!string.IsNullOrEmpty(message.ReceiptHandle))
                    {
                        Console.WriteLine($"Receiever {state}     ReceiptHandle: {message.ReceiptHandle}");
                    }
                    if (!string.IsNullOrEmpty(message.MD5OfBody))
                    {
                        Console.WriteLine($"Receiever {state}     MD5OfBody: {message.MD5OfBody}");
                    }
                    if (!string.IsNullOrEmpty(message.Body))
                    {
                        Console.WriteLine($"Receiever {state}     Body: {message.Body}");
                    }

                    foreach (string attributeKey in message.Attributes.Keys)
                    {
                        Console.WriteLine("  Attribute");
                        Console.WriteLine("    Name: {0}", attributeKey);
                        var value = message.Attributes[attributeKey];
                        Console.WriteLine("    Value: {0}", string.IsNullOrEmpty(value) ? "(no value)" : value);
                    }
                }

                var messageRecieptHandle = receiveMessageResponse.Messages[0].ReceiptHandle;

                if (_receieverShouldDeleteMessage)
                {
                    //Deleting a message
                    Console.WriteLine("Deleting the message.\n");
                    var deleteRequest = new DeleteMessageRequest { QueueUrl = _myQueueUrl, ReceiptHandle = messageRecieptHandle };
                    _sqs.DeleteMessage(deleteRequest);
                }
            }
        }
    }
}