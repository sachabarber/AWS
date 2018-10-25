using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.StepFunctions;
using Amazon;
using Newtonsoft.Json;
using Amazon.StepFunctions.Model;
using System.Net;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.Threading;

namespace SimpleStepFunction.Tests
{
    public class FunctionTest
    {
        public FunctionTest()
        {
        }

        [Fact]
        public void TestGreeting()
        {
            TestLambdaContext context = new TestLambdaContext();

            StepFunctionTasks functions = new StepFunctionTasks();

            var state = new State
            {
                Name = "MyStepFunctions"
            };


            state = functions.Greeting(state, context);

            Assert.Equal(5, state.WaitInSeconds);
            Assert.Equal("Hello MyStepFunctions", state.Message);
        }

        [Fact]
        public async Task ActualSchedulingEngineStepFunctionCallTest()
        {
            var amazonStepFunctionsConfig = new AmazonStepFunctionsConfig { RegionEndpoint = RegionEndpoint.EUWest2 };
            var assumedCredentials = await ManualAssume();

            using (var amazonStepFunctionsClient = new AmazonStepFunctionsClient(
                assumedCredentials.AccessKeyId,
                assumedCredentials.SecretAccessKey, amazonStepFunctionsConfig))
            {
                var state = new State
                {
                    Name = "MyStepFunctions"
                };
                var jsonData1 = JsonConvert.SerializeObject(state);
                var startExecutionRequest = new StartExecutionRequest
                {
                    Input = jsonData1,
                    Name = $"SchedulingEngine_{Guid.NewGuid().ToString("N")}",
                    StateMachineArn = "arn:aws:states:eu-west-2:<SomeNumber>:stateMachine:StateMachine-z8hrOwmL9CiG"
                };
                var taskStartExecutionResponse = await amazonStepFunctionsClient.StartExecutionAsync(startExecutionRequest);
                Assert.Equal(HttpStatusCode.OK, taskStartExecutionResponse.HttpStatusCode);
            }

            
        }

        async Task<Credentials> ManualAssume()
        {
            ////https://csharp.hotexamples.com/examples/Amazon.SecurityToken.Model/AssumeRoleRequest/-/php-assumerolerequest-class-examples.html
            //var basicCreds = new BasicAWSCredentials(
            //    "", 
            //    "");
            //using (var stsClient = new AmazonSecurityTokenServiceClient(basicCreds,RegionEndpoint.EUWest2))
            using (var stsClient = new AmazonSecurityTokenServiceClient(RegionEndpoint.EUWest2))
            {
                Credentials credentials = null;


                var assumeRoleRequest = new AssumeRoleRequest
                {
                    RoleArn = "arn:aws:iam::464534050515:role/SimpleStepFunction-StateMachineRole-1N638DC3RLA16",
                    RoleSessionName = Guid.NewGuid().ToString("N")
                };


                bool retry;
                int sleepSeconds = 3;

                DateTime startTime = DateTime.Now;
                do
                {
                    try
                    {
                        AssumeRoleResponse assumeRoleResponse = await stsClient.AssumeRoleAsync(assumeRoleRequest);
                        credentials = assumeRoleResponse.Credentials;

                        retry = false;
                    }
                    catch (AmazonServiceException ase)
                    {
                        if (ase.ErrorCode.Equals("AccessDenied"))
                        {
                            if (sleepSeconds > 20)
                            {
                                // If we've gotten here it's because we've retried a few times and are still getting the same error.
                                // Just rethrow the error to stop waiting. The exception will bubble up.
                                Console.WriteLine(" [Aborted AssumeRole Operation]");
                                retry = false;
                            }
                            else
                            {
                                // Write a period to the screen so we have a visual indication that we're in our retry logic. 
                                Console.Write(".");
                                // Sleep before retrying.
                                Thread.Sleep(TimeSpan.FromSeconds(sleepSeconds));
                                // Increment the retry interval.
                                sleepSeconds = sleepSeconds * 3;
                                retry = true;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                } while (retry);

                return credentials;
            }
        }
    }
}






    