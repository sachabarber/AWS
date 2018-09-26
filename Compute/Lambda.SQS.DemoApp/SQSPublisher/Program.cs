using System;
using Amazon.SQS;
using Amazon.SQS.Model;
using Nito.AsyncEx;

namespace SQSSPublisher
{
    class Program
    {
        private static bool _receieverShouldDeleteMessage = false;
        private static AmazonSQSClient _sqs = new AmazonSQSClient();
        private static string _myQueueUrl;
        private static string _queueName = "lamda-sqs-demo-app";

        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
        {
            try
            {

                var listQueuesRequest = new ListQueuesRequest();
                var listQueuesResponse = await _sqs.ListQueuesAsync(listQueuesRequest);

                try
                {
                    Console.WriteLine($"Checking for a queue called {_queueName}.\n");
                    var resp = await _sqs.GetQueueUrlAsync(_queueName);
                    _myQueueUrl = resp.QueueUrl;

                }
                catch(QueueDoesNotExistException quex)
                {
                    //Creating a queue
                    Console.WriteLine($"Create a queue called {_queueName}.\n");
                    var sqsRequest = new CreateQueueRequest { QueueName = _queueName };
                    var createQueueResponse = await _sqs.CreateQueueAsync(sqsRequest);
                    _myQueueUrl = createQueueResponse.QueueUrl;
                }

                //Sending a message
                for (int i = 0; i < 2; i++)
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


        
    }
}