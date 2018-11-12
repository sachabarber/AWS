using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

namespace SwfWorker
{
    class Program
    {
        static string domainName = "HelloWorldDomain";
        static IAmazonSimpleWorkflow SwfClient = AWSClientFactory.CreateAmazonSimpleWorkflowClient();

        public static void Main(string[] args)
        {
            string tasklistName = args[0];
            Console.Title = tasklistName.ToUpper();
            Task.Run(() => Worker(tasklistName));
            Console.Read();
        }


        static void Worker(string tasklistName)
        {
            string prefix = string.Format("Worker{0}:{1:x} ", tasklistName,
                                  System.Threading.Thread.CurrentThread.ManagedThreadId);
            while (true)
            {
                Console.WriteLine($"{prefix} : Polling for activity task ...");
                var pollForActivityTaskRequest =
                    new PollForActivityTaskRequest()
                    {
                        Domain = domainName,
                        TaskList = new TaskList()
                        {
                            // Poll only the tasks assigned to me
                            Name = tasklistName
                        }
                    };

                var pollForActivityTaskResponse =        
                    SwfClient.PollForActivityTask(pollForActivityTaskRequest);

                if (pollForActivityTaskResponse.ActivityTask.ActivityId == null)
                {
                    Console.WriteLine($"{prefix} : NULL");
                }
                else
                {
                    Console.WriteLine($"Worker saw Input {pollForActivityTaskResponse.ActivityTask.Input}");

                    var respondActivityTaskCompletedRequest = new RespondActivityTaskCompletedRequest()
                        {
                            Result = "{\"activityResult1\":\"Result Value1\"}",
                            TaskToken = pollForActivityTaskResponse.ActivityTask.TaskToken
                        };

                    var respondActivityTaskCompletedResponse =
                        SwfClient.RespondActivityTaskCompleted(respondActivityTaskCompletedRequest);
                    Console.WriteLine($"{prefix} : Activity task completed. ActivityId - " +
                        pollForActivityTaskResponse.ActivityTask.ActivityId);
                }
            }
        }


    }
}