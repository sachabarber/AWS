using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

namespace SwfInitiator
{
    class Program
    {
        static string domainName = "HelloWorldDomain";
        static IAmazonSimpleWorkflow SwfClient = AWSClientFactory.CreateAmazonSimpleWorkflowClient();

        public static void Main(string[] args)
        {
            string workflowName = "HelloWorld Workflow";

            // Setup
            RegisterDomain();
            RegisterActivity("Activity1A", "Activity1");
            RegisterActivity("Activity1B", "Activity1");
            RegisterActivity("Activity2", "Activity2");
            RegisterWorkflow(workflowName);

            //// Launch workers to service Activity1A and Activity1B
            ////  This is acheived by sharing same tasklist name (i.e.) "Activity1"
            StartProcess(@"..\..\..\Worker\bin\Debug\SwfWorker", new[] { "Activity1" });
            StartProcess(@"..\..\..\Worker\bin\Debug\SwfWorker", new[] { "Activity1" });

            //// Launch Workers for Activity2
            StartProcess(@"..\..\..\Worker\bin\Debug\SwfWorker", new[] { "Activity2" });

            //// Start the Deciders, which defines the structure/flow of Workflow
            StartProcess(@"..\..\..\Decider\bin\Debug\SwfDecider", null);

            //Start the workflow
            Task.Run(() => StartWorkflow(workflowName));

            Console.Read();
        }

        static void StartProcess(string processLocation, string[] args)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = processLocation;
            if (args != null)
                p.StartInfo.Arguments = String.Join(" ", args);
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            p.Start();
            p.WaitForExit();
        }

        static void RegisterDomain()
        {
            // Register if the domain is not already registered.
            var listDomainRequest = new ListDomainsRequest()
            {
                RegistrationStatus = RegistrationStatus.REGISTERED
            };

            if (SwfClient.ListDomains(listDomainRequest).DomainInfos.Infos.FirstOrDefault(
                                                      x => x.Name == domainName) == null)
            {
                var request = new RegisterDomainRequest()
                {
                    Name = domainName,
                    Description = "Hello World Demo",
                    WorkflowExecutionRetentionPeriodInDays = "1"
                };

                Console.WriteLine("Setup: Created Domain - " + domainName);
                SwfClient.RegisterDomain(request);
            }
        }

        static void RegisterActivity(string name, string tasklistName)
        {
            // Register activities if it is not already registered
            var listActivityRequest = new ListActivityTypesRequest()
            {
                Domain = domainName,
                Name = name,
                RegistrationStatus = RegistrationStatus.REGISTERED
            };

            if (SwfClient.ListActivityTypes(listActivityRequest).ActivityTypeInfos.TypeInfos.FirstOrDefault(
                                          x => x.ActivityType.Version == "2.0") == null)
            {
                var request = new RegisterActivityTypeRequest()
                {
                    Name = name,
                    Domain = domainName,
                    Description = "Hello World Activities",
                    Version = "2.0",
                    DefaultTaskList = new TaskList() { Name = tasklistName },//Worker poll based on this
                    DefaultTaskScheduleToCloseTimeout = "300",
                    DefaultTaskScheduleToStartTimeout = "150",
                    DefaultTaskStartToCloseTimeout = "450",
                    DefaultTaskHeartbeatTimeout = "NONE",
                };
                SwfClient.RegisterActivityType(request);
                Console.WriteLine($"Setup: Created Activity Name - {request.Name}");
            }
        }

        static void RegisterWorkflow(string name)
        {
            // Register workflow type if not already registered
            var listWorkflowRequest = new ListWorkflowTypesRequest()
            {
                Name = name,
                Domain = domainName,
                RegistrationStatus = RegistrationStatus.REGISTERED
            };

            if (SwfClient.ListWorkflowTypes(listWorkflowRequest)
                .WorkflowTypeInfos
                .TypeInfos
                .FirstOrDefault(x => x.WorkflowType.Version == "2.0") == null)
            {
                var request = new RegisterWorkflowTypeRequest()
                {
                    DefaultChildPolicy = ChildPolicy.TERMINATE,
                    DefaultExecutionStartToCloseTimeout = "300",
                    DefaultTaskList = new TaskList()
                    {
                        Name = "HelloWorld" // Decider need to poll for this task
                    },
                    DefaultTaskStartToCloseTimeout = "150",
                    Domain = domainName,
                    Name = name,
                    Version = "2.0"
                };

                SwfClient.RegisterWorkflowType(request);

                Console.WriteLine($"Setup: Registerd Workflow Name - {request.Name}");
            }
        }

        static void StartWorkflow(string name)
        {
            string workflowID = $"Hello World WorkflowID - {DateTime.Now.Ticks.ToString()}";
            SwfClient.StartWorkflowExecution(new StartWorkflowExecutionRequest()
            {
                Input = "{\"inputparam1\":\"value1\"}", // Serialize input to a string
                WorkflowId = workflowID,
                Domain = domainName,
                WorkflowType = new WorkflowType()
                {
                    Name = name,
                    Version = "2.0"
                }
            });
            Console.WriteLine($"Setup: Workflow Instance created ID={workflowID}");
        }
    }
}