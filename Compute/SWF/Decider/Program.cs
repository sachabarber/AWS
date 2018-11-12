using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

namespace SwfDeciderDecider
{
    class Program
    {
        static string domainName = "HelloWorldDomain";
        static IAmazonSimpleWorkflow SwfDeciderClient =
                    AWSClientFactory.CreateAmazonSimpleWorkflowClient();

        public static void Main(string[] args)
        {
            Console.Title = "DECIDER";
            // Start the Deciders, which defines the structure/flow of Workflow
            Task.Run(() => Decider());
            Console.Read();
        }


        // Simple logic
        //  Creates four activities at the begining
        //  Waits for them to complete and completes the workflow
        static void Decider()
        {
            int activityCount = 0; // This refers to total number of activities per workflow
            while (true)
            {
                Console.WriteLine("Decider: Polling for decision task ...");
                var request = new PollForDecisionTaskRequest()
                {
                    Domain = domainName,
                    TaskList = new TaskList() { Name = "HelloWorld" }
                };

                var response = SwfDeciderClient.PollForDecisionTask(request);
                if (response.DecisionTask.TaskToken == null)
                {
                    Console.WriteLine("Decider: NULL");
                    continue;
                }

                int completedActivityTaskCount = 0, totalActivityTaskCount = 0;
                foreach (HistoryEvent e in response.DecisionTask.Events)
                {
                    Console.WriteLine($"Decider: EventType - {e.EventType}" +
                        $", EventId - {e.EventId}");
                    if (e.EventType == "ActivityTaskCompleted")
                        completedActivityTaskCount++;
                    if (e.EventType.Value.StartsWith("Activity"))
                        totalActivityTaskCount++;
                }
                Console.WriteLine($".... completedCount={completedActivityTaskCount}");

                var decisions = new List<Decision>();
                if (totalActivityTaskCount == 0) // Create this only at the begining
                {
                    ScheduleActivity("Activity1A", decisions);
                    ScheduleActivity("Activity1B", decisions);
                    ScheduleActivity("Activity2", decisions);
                    ScheduleActivity("Activity2", decisions);
                    activityCount = 4;
                }
                else if (completedActivityTaskCount == activityCount)
                {
                    var decision = new Decision()
                    {
                        DecisionType = DecisionType.CompleteWorkflowExecution,
                        CompleteWorkflowExecutionDecisionAttributes =
                          new CompleteWorkflowExecutionDecisionAttributes
                          {
                              Result = "{\"Result\":\"WF Complete!\"}"
                          }
                    };
                    decisions.Add(decision);

                    Console.WriteLine("Decider: WORKFLOW COMPLETE!!!!!!!!!!!!!!!!!!!!!!");
                }
                var respondDecisionTaskCompletedRequest =
                    new RespondDecisionTaskCompletedRequest()
                    {
                        Decisions = decisions,
                        TaskToken = response.DecisionTask.TaskToken
                    };
                SwfDeciderClient.RespondDecisionTaskCompleted(respondDecisionTaskCompletedRequest);
            }
        }

        static void ScheduleActivity(string name, List<Decision> decisions)
        {
            var decision = new Decision()
            {
                DecisionType = DecisionType.ScheduleActivityTask,
                ScheduleActivityTaskDecisionAttributes =  
                  new ScheduleActivityTaskDecisionAttributes()
                  {
                      ActivityType = new ActivityType()
                      {
                          Name = name,
                          Version = "2.0"
                      },
                      ActivityId = name + "-" + System.Guid.NewGuid().ToString(),
                      Input = "{\"activityInput1\":\"value1\"}"
                  }
            };
            Console.WriteLine($"Decider: ActivityId={decision.ScheduleActivityTaskDecisionAttributes.ActivityId}");
            decisions.Add(decision);
        }
    }
}